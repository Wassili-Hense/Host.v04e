using NiL.JS.Core;
using NiL.JS.Core.Modules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace X13.PLC {
  public class PLC {
    static PLC() {
      instance = new PLC();
    }
    public static readonly PLC instance;

    private ConcurrentQueue<Perform> _tcQueue;
    private List<Perform> _prOp;
    private int _busyFlag;
    private Topic _sign1;
    private Topic _sign2;
    private bool _signFl;
    private int _pfPos;

    private List<PiBlock> _blocks;
    private Dictionary<Topic, PiVar> _vars;
    private List<PiVar> _rLayerVars;
    public Topic sign { get { return _signFl ? _sign1 : _sign2; } }
    internal Topic signAlt { get { return _signFl ? _sign2 : _sign1; } }

    public PLC() {
      _blocks = new List<PiBlock>();
      _vars = new Dictionary<Topic, PiVar>();
      _tcQueue = new ConcurrentQueue<Perform>();
      _rLayerVars = new List<PiVar>();
      _prOp = new List<Perform>(128);
      _busyFlag = 1;
    }
    public void Init() {
      if(Topic.root.children.Any()) {
        this.Clear();
      }
      _sign1 = Topic.root.Get("/etc/plugins/PLC/sign1");
      _sign1.local = true;
      _sign1.saved = false;
      _sign2 = Topic.root.Get("/etc/plugins/PLC/sign2");
      _sign2.local = true;
      _sign2.saved = false;
    }
    public void Start() {
    }
    public void Stop() {
      _blocks.Clear();
      _vars.Clear();
    }

    internal void Tick() {
      if(Interlocked.CompareExchange(ref _busyFlag, 2, 1) != 1) {
        return;
      }
      Perform cmd;
      _pfPos = 0;
      while(_tcQueue.TryDequeue(out cmd)) {
        if(cmd == null || cmd.src == null) {
          continue;
        }
        TickStep1(cmd);
      }

      for(_pfPos = 0; _pfPos < _prOp.Count; _pfPos++) {
        TickStep2(_prOp[_pfPos]);
      }

      for(_pfPos = 0; _pfPos < _prOp.Count; _pfPos++) {
        cmd = _prOp[_pfPos];
        if(cmd.art == Perform.Art.changed || cmd.art == Perform.Art.remove) {
          if(cmd.old_o != null) {
            ITenant it;
            if((it = cmd.old_o as ITenant) != null) {
              it.owner = null;
            }
          }
        }
        if(cmd.art == Perform.Art.changed || cmd.art == Perform.Art.create) {
          if(cmd.src._value != null && !cmd.src.disposed) {
            ITenant tt;
            if((tt = cmd.src._value as ITenant) != null) {
              tt.owner = cmd.src;
            }
          }
        }
        if(cmd.art != Perform.Art.set) {
          cmd.src.Publish(cmd);
        }
        //if(cmd.src.disposed) {
        //  cmd.src._flags[3]=true;
        //}
      }
      if(_rLayerVars.Any()) {
        CalcLayers(new Queue<PiVar>(_rLayerVars.Where(z => !z.ip)));
        if(_rLayerVars.Any(z => z.layer == 0)) {
          CalcLayers(new Queue<PiVar>(_rLayerVars.Where(z => z.layer == 0)));
        }
        _rLayerVars.Clear();
      }
      _prOp.Clear();
      _signFl = !_signFl;
      _busyFlag = 1;
    }
    private void TickStep1(Perform c) {
      Action<Topic, Perform> func;
      Topic t;
      switch(c.art) {
      case Perform.Art.create:
        if((t = c.src.parent) != null) {
          t._children[c.src.name]=c.src;
          if(t._subRecords != null) {
            foreach(var sr in t._subRecords.Where(z => z.ma != null && z.ma.Length == 1 && z.ma[0] == Topic.Bill.maskChildren)) {
              c.src.Subscribe(new Topic.SubRec() { mask = sr.mask, ma = new string[0], f = sr.f });
            }
          }
          while(t != null) {
            if(t._subRecords != null) {
              foreach(var sr in t._subRecords.Where(z => z.ma != null && z.ma.Length == 1 && z.ma[0] == Topic.Bill.maskAll)) {
                c.src.Subscribe(new Topic.SubRec() { mask = sr.mask, ma = new string[0], f = sr.f });
              }
            }
            t = t.parent;
          }
        }
        EnquePerf(c);
        break;
      case Perform.Art.subscribe:
      case Perform.Art.unsubscribe:
        if((func = c.o as Action<Topic, Perform>) != null) {
          if(c.i == 0) {
            if(c.art == Perform.Art.subscribe) {
              c.src.Subscribe(new Topic.SubRec() { mask = c.src.path, ma = Topic.Bill.curArr, f = func });
            } else {
              c.src.Unsubscribe(c.src.path, func);
            }
            goto case Perform.Art.set;
          } else {
            Topic.SubRec sr;
            Topic.Bill b;
            if(c.i == 1) {
              sr = new Topic.SubRec() { mask = c.prim.path + "/+", ma = Topic.Bill.curArr, f = func };
              if(c.art == Perform.Art.subscribe) {
                c.src.Subscribe(new Topic.SubRec() { mask = sr.mask, ma = Topic.Bill.childrenArr, f = func });
              } else {
                c.src.Unsubscribe(sr.mask, func);
              }
              b = c.src.children;
            } else {
              sr = new Topic.SubRec() { mask = c.prim.path + "/#", ma = Topic.Bill.allArr, f = func };
              b = c.src.all;
            }
            foreach(Topic tmp in b) {
              if(c.art == Perform.Art.subscribe) {
                tmp.Subscribe(sr);
              } else {
                c.src.Unsubscribe(sr.mask, func);
              }
              EnquePerf(Perform.Create(tmp, c.art, c.src));
            }
          }
        }
        break;

      case Perform.Art.remove:
        foreach(Topic tmp in c.src.all) {
          EnquePerf(Perform.Create(tmp, c.art, c.prim));
        }
        break;
      case Perform.Art.move:
        if((t = c.o as Topic) != null) {
          string oPath = c.src.path;
          string nPath = t.path;
          t._children = c.src._children;
          c.src._children = null;
          t._value = c.src._value;
          c.src._value = JSObject.Undefined;
          if(c.src._subRecords != null) {
            foreach(var sr in c.src._subRecords) {
              if(sr.mask.StartsWith(oPath)) {
                t.Subscribe(new Topic.SubRec() { mask = sr.mask.Replace(oPath, nPath), ma = sr.ma, f = sr.f });
              }
            }
          }
          foreach(var t1 in t.children) {
            t1.parent = t;
          }
          foreach(var t1 in t.all) {
            if(t1._subRecords != null) {
              for(int i = t1._subRecords.Count - 1; i >= 0; i--) {
                if(t1._subRecords[i].mask.StartsWith(oPath)) {
                  t1._subRecords[i] = new Topic.SubRec() { mask = t1._subRecords[i].mask.Replace(oPath, nPath), ma = t1._subRecords[i].ma, f = t1._subRecords[i].f };
                } else if(!t1._subRecords[i].mask.StartsWith(nPath)) {
                  t1._subRecords.RemoveAt(i);
                }
              }
            }
            t1.path = t1.parent == Topic.root ? string.Concat("/", t1.name) : string.Concat(t1.parent.path, "/", t1.name);
            EnquePerf(Perform.Create(t1, Perform.Art.create, c.prim));
          }

          int idx = EnquePerf(c);
          if(idx > 0) {
            Perform c1 = _prOp[idx - 1];
            if(c1.src == c.src && c1.art == Perform.Art.set) {
              var p = Perform.Create(t, Perform.Art.set, c1.prim);
              p.o = c1.o;
              p.i = c1.i;
              EnquePerf(p);
            }
          }
        }
        break;
      case Perform.Art.changed:
      case Perform.Art.set:
      case Perform.Art.setJson:
        EnquePerf(c);
        break;
      }
    }
    private void TickStep2(Perform cmd) {
      if(cmd.art == Perform.Art.remove || cmd.art == Perform.Art.setJson || (cmd.art == Perform.Art.set && !object.Equals(cmd.src._value, cmd.o))) {
        cmd.old_o = cmd.src._value;
        if(cmd.art == Perform.Art.setJson) {
          var jso= JSON.parse((string)cmd.o);
          JSObject ty;
          if(jso.ValueType==JSObjectType.Object && (ty=jso.GetMember("$type")).IsDefinded) {
            switch(ty.As<string>()) {
            case "PiBlock":
              cmd.src._value=new PiBlock(jso["func"].As<string>());
              break;
            case "PiLink":
              cmd.src._value=new PiLink(cmd.src.parent.Get(jso["i"].As<string>(), true, cmd.prim), cmd.src.parent.Get(jso["o"].As<string>(), true, cmd.prim));
              break;
            case "PiAlias":
              cmd.src._value=new PiAlias(cmd.src.Get(jso["alias"].As<string>(), true, cmd.prim));
              break;
            case "PiDeclarer":
              cmd.src._value=new PiDeclarer(jso);
              break;
            default:
              X13.lib.Log.Warning("{0}.setJson({1}) - unknown $type", cmd.src.path, cmd.o);
              break;
            }
          } else {
            cmd.src._value = jso;
          }
        } else {
          switch(Type.GetTypeCode(cmd.o==null?null:cmd.o.GetType())) {
          case TypeCode.Boolean:
            cmd.src._value=new NiL.JS.Core.BaseTypes.Boolean((bool)cmd.o);
            break;
          case TypeCode.Byte:
          case TypeCode.SByte:
          case TypeCode.Int16:
          case TypeCode.Int32:
          case TypeCode.UInt16:
            cmd.src._value=new NiL.JS.Core.BaseTypes.Number(Convert.ToInt32(cmd.o));
            break;
          case TypeCode.Int64:
          case TypeCode.UInt32:
          case TypeCode.UInt64:
            cmd.src._value=new NiL.JS.Core.BaseTypes.Number(Convert.ToInt64(cmd.o));
            break;
          case TypeCode.Single:
          case TypeCode.Double:
          case TypeCode.Decimal:
            cmd.src._value=new NiL.JS.Core.BaseTypes.Number(Convert.ToDouble(cmd.o));
            break;
          case TypeCode.DateTime: {
              var dt = ((DateTime)cmd.o);
              var jdt=new NiL.JS.Core.BaseTypes.Date(new NiL.JS.Core.Arguments { dt.Year, dt.Month, dt.Year, dt.Hour, dt.Minute, dt.Second, dt.Millisecond });
              cmd.src._value=jdt.getTime();
            }
            break;
          case TypeCode.Empty:
            cmd.src._value=JSObject.Undefined;
            break;
          case TypeCode.String:
            cmd.src._value=new NiL.JS.Core.BaseTypes.String((string)cmd.o);
            break;
          case TypeCode.Object:
          default: {
              JSObject jo;
              if((jo = cmd.o as JSObject)!=null) {
                cmd.src._value=jo;
              } else {
                cmd.src._value.Assign(new JSObject(cmd.o));
              }
            }
            break;
          }
          cmd.src._json = null;
        }
        if(cmd.art != Perform.Art.remove) {
          cmd.art = Perform.Art.changed;
        }
      }
      if(cmd.art == Perform.Art.remove || cmd.art == Perform.Art.move) {
        cmd.src.disposed = true;
        if(cmd.src.parent != null) {
          cmd.src.parent._children.Remove(cmd.src.name);
        }
      }
      //TODO: save for undo/redo
      /*IHistory h;
      if(cmd.prim!=null && cmd.prim._vt==VT.Object && (h=cmd.prim._o as IHistory)!=null) {
        h.Add(cmd);
      }*/
    }

    private void Clear() {
      lock(Topic.root) {
        Perform c;
        while(_tcQueue.TryDequeue(out c)) {
        }
        _prOp.Clear();
        foreach(var t in Topic.root.all) {
          t.disposed = true;
          if(t._children != null) {
            t._children.Clear();
            t._children = null;
          }
        }
        _busyFlag = 1;
      }
      Topic.root.disposed = false;
    }
    internal void DoCmd(Perform cmd) {
      if(cmd.prim==null || (cmd.prim._value as PlcItem)==null) {
        _tcQueue.Enqueue(cmd);
      } else {
        int idx = _prOp.BinarySearch(cmd);
        if(idx < 0) {
          idx = ~idx;
          if(idx <= _pfPos) {
            _tcQueue.Enqueue(cmd);               // Published in next tick
            return;
          }
        } else {
          if(idx >= _pfPos) {
            _tcQueue.Enqueue(cmd);               // Published in next tick
            return;
          }
          var oCmd = _prOp[idx];
          if(oCmd.art == Perform.Art.changed) {
            cmd.old_o = oCmd.old_o;
          }
        }
        TickStep1(cmd);
        TickStep2(cmd);
      }
    }

    private int EnquePerf(Perform cmd) {
      int idx = _prOp.BinarySearch(cmd);
      if(idx < 0) {
        idx = ~idx;
        _prOp.Insert(idx, cmd);
      } else {
        var a1 = (int)_prOp[idx].art;
        if(((int)cmd.art) >= a1) {
          _prOp[idx] = cmd;
        } else {
          idx = ~idx;
        }
      }
      return idx;
    }
    internal void AddBlock(PiBlock bl) {
      _blocks.Add(bl);
    }
    internal PiVar GetVar(Topic t, bool create, bool refresh=false) {
      PiVar v;
      if(!_vars.TryGetValue(t, out v)) {
        if(create) {
          v = new PiVar(t);
          _vars[t] = v;
          _rLayerVars.Add(v);
        } else {
          v = null;
        }
      } else if(refresh) {
        _rLayerVars.Add(v);
      }
      return v;
    }
    private void CalcLayers(Queue<PiVar> vQu) {
      PiVar v1;

      do {
        while(vQu.Count > 0) {
          v1 = vQu.Dequeue();
          if(v1.layer == 0) {
            v1.calcPath = new PiBlock[0];
            if(v1.block != null && v1.block.layer == 0 && v1.op) {
              continue;
            }
            v1.layer = 1;
          }
          foreach(var l in v1._cont.Select(z => z as PiLink).Where(z => z!=null && z.input == v1)) {
            l.layer = v1.layer;
            l.output.layer = l.layer;
            l.output.calcPath = v1.calcPath;
            vQu.Enqueue(l.output);
          }
          if(v1.ip && v1.block != null) {
            if(v1.calcPath.Contains(v1.block)) {
              if(v1.layer > 0) {
                v1.layer = -v1.layer;
              }
              X13.lib.Log.Debug("{0} make loop", v1.owner.path);
            } else if(v1.block._pins.Where(z => z.Value.ip).All(z => z.Value.layer >= 0)) {
              v1.block.layer = v1.block._pins.Where(z => z.Value.ip).Max(z => z.Value.layer) + 1;
              v1.block.calcPath = v1.block.calcPath.Union(v1.calcPath).ToArray();
              foreach(var v3 in v1.block._pins.Where(z => z.Value.op).Select(z => z.Value)) {
                v3.layer = v1.block.layer;
                v3.calcPath = v1.block.calcPath;
                if(!vQu.Contains(v3)) {
                  vQu.Enqueue(v3);
                }
              }
            }
          }
        }
        if(vQu.Count == 0 && _blocks.Any(z => z.layer == 0)) { // break a one loop in the graph
          var bl = _blocks.Where(z => z.layer <= 0).Min();
          foreach(var ip in bl._pins.Select(z => z.Value).Where(z => z.ip && z.layer > 0)) {
            bl.calcPath = bl.calcPath.Union(ip.calcPath).ToArray();
          }
          bl.layer = bl._pins.Select(z => z.Value).Where(z => !z.op && z.layer > 0).Max(z => z.layer) + 1;
          foreach(var v3 in bl._pins.Select(z => z.Value).Where(z => z.op)) {
            v3.layer = bl.layer;
            v3.calcPath = bl.calcPath;
            if(!vQu.Contains(v3)) {
              vQu.Enqueue(v3);
            }
          }
        }
      } while(vQu.Count > 0);
    }

    internal void DelVar(PiVar v) {
      _vars.Remove(v.owner);
    }
  }
}
