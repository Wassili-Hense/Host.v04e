using Jurassic;
using Jurassic.Library;
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
      instance=new PLC();
    }
    public static readonly PLC instance;

    private ConcurrentQueue<Perform> _tcQueue;
    private List<Perform> _prOp;
    private int _busyFlag;
    private Topic _sign1;
    private Topic _sign2;
    private bool _signFl;
    private int _pfPos;
    private Lazy<ScriptEngine> _eng;

    private List<PiBlock> _blocks;
    private Dictionary<Topic, PiVar> _vars;
    public Topic sign { get { return _signFl?_sign1:_sign2; } }
    internal Topic signAlt { get { return _signFl?_sign2:_sign1; } }
    public ScriptEngine Engine { get { return _eng.Value; } }

    public PLC() {
      _blocks=new List<PiBlock>();
      _vars=new Dictionary<Topic, PiVar>();
      _tcQueue=new ConcurrentQueue<Perform>();

      _prOp=new List<Perform>(128);
      _busyFlag=1;
      _eng=new Lazy<ScriptEngine>();
    }
    public void Init() {
      _sign1=Topic.root.Get("/etc/plugins/PLC/sign1");
      _sign2=Topic.root.Get("/etc/plugins/PLC/sign2");
    }
    public void Start() {
      Queue<PiVar> vQu=new Queue<PiVar>(_vars.Values);
      PiVar v1, v2;
      while(vQu.Count>0) {
        v1=vQu.Dequeue();
        if(v1._owner.vType==typeof(Topic)) {
          if(!_vars.TryGetValue(v1._owner.As<Topic>(), out v2)) {
            v2=new PiVar(v1._owner.As<Topic>());
            _vars[v1._owner.As<Topic>()]=v2;
            vQu.Enqueue(v2);
          }
          v1.gray=true;
          var l=new PiLink(v2, v1);
          v1._links.Add(l);
          v2._links.Add(l);
        } else if(v1.dir==true) {
          v1.gray=true;
        }
      }
      vQu=new Queue<PiVar>(_vars.Values.Where(z => z.gray==false));
      do {
        while(vQu.Count>0) {
          v1=vQu.Dequeue();
          if(v1.layer==0) {
            v1.layer=1;
            v1.calcPath=new PiBlock[0];
          }
          foreach(var l in v1._links.Where(z => z.input==v1)) {
            l.layer=v1.layer;
            l.output.layer=l.layer;
            l.output.calcPath=v1.calcPath;
            vQu.Enqueue(l.output);
          }
          if(v1.dir==false && v1.block!=null) {
            if(v1.calcPath.Contains(v1.block)) {
              if(v1.layer>0) {
                v1.layer=-v1.layer;
              }
              X13.lib.Log.Debug("{0} make loop", v1._owner.path);
            } else if(v1.block._pins.Where(z => z.Value.dir==false).All(z => z.Value.layer>=0)) {
              v1.block.layer=v1.block._pins.Where(z => z.Value.dir==false).Max(z => z.Value.layer)+1;
              v1.block.calcPath=v1.block.calcPath.Union(v1.calcPath).ToArray();
              foreach(var v3 in v1.block._pins.Where(z => z.Value.dir==true).Select(z => z.Value)) {
                v3.layer=v1.block.layer;
                v3.calcPath=v1.block.calcPath;
                if(!vQu.Contains(v3)) {
                  vQu.Enqueue(v3);
                }
              }
            }
          }
        }
        if(vQu.Count==0 && _blocks.Any(z => z.layer==0)) { // break a one loop in the graph
          var bl=_blocks.Where(z => z.layer<0).Min();
          foreach(var ip in bl._pins.Select(z => z.Value).Where(z => z.dir==false && z.layer>0)) {
            bl.calcPath=bl.calcPath.Union(ip.calcPath).ToArray();
          }
          bl.layer=bl._pins.Select(z => z.Value).Where(z => z.dir==false && z.layer>0).Max(z => z.layer)+1;
          foreach(var v3 in bl._pins.Select(z => z.Value).Where(z => z.dir==true)) {
            v3.layer=bl.layer;
            v3.calcPath=bl.calcPath;
            if(!vQu.Contains(v3)) {
              vQu.Enqueue(v3);
            }
          }
        }
      } while(vQu.Count>0);
    }
    public void Stop() {
      _blocks.Clear();
      _vars.Clear();
    }

    internal void Tick() {
      if(Interlocked.CompareExchange(ref _busyFlag, 2, 1)!=1) {
        return;
      }
      Perform c;
      Action<Topic, Perform> func;
      Topic t;
      while(_tcQueue.TryDequeue(out c)) {
        if(c==null || c.src==null) {
          continue;
        }
        switch(c.art) {
        case Perform.Art.create:
          if((t=c.src.parent)!=null) {
            if(t._subRecords!=null) {
              foreach(var sr in t._subRecords.Where(z => z.ma!=null && z.ma.Length==1 && z.ma[0]==Topic.Bill.maskChildren)) {
                c.src.Subscribe(new Topic.SubRec() { mask=sr.mask, ma=new string[0], f=sr.f });
              }
            }
            while(t!=null) {
              if(t._subRecords!=null) {
                foreach(var sr in t._subRecords.Where(z => z.ma!=null && z.ma.Length==1 && z.ma[0]==Topic.Bill.maskAll)) {
                  c.src.Subscribe(new Topic.SubRec() { mask=sr.mask, ma=new string[0], f=sr.f });
                }
              }
              t=t.parent;
            }
          }
          EnquePerf(c);
          break;
        case Perform.Art.subscribe:
        case Perform.Art.unsubscribe:
          if((func=c.o as Action<Topic, Perform>)!=null) {
            if(c.dt.l==0) {
              if(c.art==Perform.Art.subscribe) {
                c.src.Subscribe(new Topic.SubRec() { mask=c.src.path, ma=Topic.Bill.curArr, f=func });
              } else {
                c.src.Unsubscribe(c.src.path, func);
              }
              goto case Perform.Art.set;
            } else {
              Topic.SubRec sr;
              Topic.Bill b;
              if(c.dt.l==1) {
                sr=new Topic.SubRec() { mask=c.prim.path+"/+", ma=Topic.Bill.curArr, f=func };
                if(c.art==Perform.Art.subscribe) {
                  c.src.Subscribe(new Topic.SubRec() { mask=sr.mask, ma=Topic.Bill.childrenArr, f=func });
                } else {
                  c.src.Unsubscribe(sr.mask, func);
                }
                b=c.src.children;
              } else {
                sr=new Topic.SubRec() { mask=c.prim.path+"/#", ma=Topic.Bill.allArr, f=func };
                b=c.src.all;
              }
              foreach(Topic tmp in b) {
                if(c.art==Perform.Art.subscribe) {
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
          if((t=c.o as Topic)!=null) {
            string oPath=c.src.path;
            string nPath=t.path;
            t._children=c.src._children;
            c.src._children=null;
            t._vt=c.src._vt;
            c.src._vt=Topic.VT.Undefined;
            t._dt=c.src._dt;
            t._o=c.src._o;
            c.src._o=null;
            if(c.src._subRecords!=null) {
              foreach(var sr in c.src._subRecords) {
                if(sr.mask.StartsWith(oPath)) {
                  t.Subscribe(new Topic.SubRec() { mask=sr.mask.Replace(oPath, nPath), ma=sr.ma, f=sr.f });
                }
              }
            }
            foreach(var t1 in t.children) {
              t1._parent=t;
            }
            foreach(var t1 in t.all) {
              if(t1._subRecords!=null) {
                for(int i=t1._subRecords.Count-1; i>=0; i--) {
                  if(t1._subRecords[i].mask.StartsWith(oPath)) {
                    t1._subRecords[i]=new Topic.SubRec() { mask=t1._subRecords[i].mask.Replace(oPath, nPath), ma=t1._subRecords[i].ma, f=t1._subRecords[i].f };
                  } else if(!t1._subRecords[i].mask.StartsWith(nPath)) {
                    t1._subRecords.RemoveAt(i);
                  }
                }
              }
              t1._path=t1.parent==Topic.root?string.Concat("/", t1.name):string.Concat(t1.parent.path, "/", t1.name);
              EnquePerf(Perform.Create(t1, Perform.Art.create, c.prim));
            }

            int idx=EnquePerf(c);
            if(idx>0) {
              Perform c1=_prOp[idx-1];
              if(c1.src==c.src && c1.art==Perform.Art.set) {
                var p=Perform.Create(t, Perform.Art.set, c1.prim);
                p.vt=c1.vt;
                p.dt=c1.dt;
                p.o=c1.o;
                EnquePerf(p);
              }
            }
          }
          break;
        case Perform.Art.changed:
        case Perform.Art.set:
          EnquePerf(c);
          break;
        }
      }

      for(_pfPos=0; _pfPos<_prOp.Count; _pfPos++) {
        var cmd=_prOp[_pfPos];
        if(cmd.art==Perform.Art.set || cmd.art==Perform.Art.remove) {
          if(cmd.art!=Perform.Art.set || cmd.src._vt!=cmd.vt || !object.Equals(cmd.src._o, cmd.o) ||  cmd.src._dt.l!=cmd.dt.l) {
            cmd.old_vt=cmd.src._vt;
            cmd.old_dt=cmd.src._dt;
            cmd.old_o=cmd.src._o;
            // json
            if(cmd.vt==Topic.VT.Json) {
              cmd.src._json=cmd.o as string;
              if(string.IsNullOrEmpty(cmd.src._json)) {
                cmd.src._vt=Topic.VT.Undefined;
                cmd.src._dt.l=0;
                cmd.src._o=null;
              } else {
                object o=JSONObject.Parse(Engine, cmd.src._json);
                var os=o as ObjectInstance;
                if(os!=null) {
                  if(os.HasProperty("$ref")) {
                    o=cmd.src.Get(os.GetPropertyValue("$ref") as string, true, cmd.prim);
                  }
                }
                Perform.Set(o, ref cmd.src._vt, ref cmd.src._o, ref cmd.src._dt);
              }
              //  /json
            } else {
              cmd.src._vt=cmd.vt;
              cmd.src._dt=cmd.dt;
              cmd.src._o=cmd.o;
              cmd.src._json=null;
            }
            if(cmd.art==Perform.Art.set) {
              cmd.art=Perform.Art.changed;
            }
          }
        }
        if(cmd.art==Perform.Art.remove || cmd.art==Perform.Art.move) {
          cmd.src._flags[2]=true;
          if(cmd.src.parent!=null) {
            cmd.src.parent._children.Remove(cmd.src.name);
          }
        }
        //TODO: save for undo/redo
        /*IHistory h;
        if(cmd.prim!=null && cmd.prim._vt==VT.Object && (h=cmd.prim._o as IHistory)!=null) {
          h.Add(cmd);
        }*/
      }

      for(_pfPos=0; _pfPos<_prOp.Count; _pfPos++) {
        var cmd=_prOp[_pfPos];
        if(cmd.art==Perform.Art.changed || cmd.art==Perform.Art.remove) {
          if(cmd.old_o!=null) {
            Topic r;
            ITenant it;
            if(cmd.old_vt==Topic.VT.Ref && (r=cmd.old_o as Topic)!=null) {
              r.Unsubscribe(r.path, cmd.src.RefChanged);
              //TODO: this.DelVar(cmd.src);
            } else if(cmd.old_vt==Topic.VT.Object && (it=cmd.old_o as ITenant)!=null) {
              it.owner=null;
            }
          }
        }
        if(cmd.art==Perform.Art.changed || cmd.art==Perform.Art.create) {
          if(cmd.src._o!=null && !cmd.src._flags[2]) {
            ITenant tt;
            Topic r;
            if(cmd.src._vt==Topic.VT.Ref && (r=cmd.src._o as Topic)!=null) {
              r.Subscribe(new Topic.SubRec() { mask=r.path, ma=Topic.Bill.curArr, f=cmd.src.RefChanged });
              this.GetVar(cmd.src, true);
            } else if(cmd.src._vt==Topic.VT.Object &&  (tt=cmd.src._o as ITenant)!=null) {
              tt.owner=cmd.src;
            }
          }
        }
        if(cmd.art!=Perform.Art.set) {
          cmd.src.Publish(cmd);
        }
        if(cmd.src._flags[2]) {
          cmd.src._flags[3]=true;
        }
      }
      _prOp.Clear();
      _signFl=!_signFl;
      _busyFlag=1;
    }
    internal void Clear() {
      lock(Topic.root) {
        Perform c;
        while(_tcQueue.TryDequeue(out c)) {
        }
        _prOp.Clear();
        foreach(var t in Topic.root.all) {
          t._flags[2]=true;
          if(t._children!=null) {
            t._children.Clear();
            t._children=null;
          }
        }
        _busyFlag=1;
      }
      Topic.root._flags[2]=false;
    }
    internal void DoCmd(Perform cmd) {
      _tcQueue.Enqueue(cmd);
    }

    internal int EnquePerf(Perform cmd) {
      int idx=_prOp.BinarySearch(cmd);
      if(idx<0) {
        idx=~idx;
        _prOp.Insert(idx, cmd);
      } else {
        var a1=(int)_prOp[idx].art;
        if(((int)cmd.art)>=a1) {
          _prOp[idx]=cmd;
        } else {
          idx=~idx;
        }
      }
      return idx;
    }
    internal void DoPlcCmd(Perform cmd) {
      if(cmd.vt==cmd.src._vt && object.Equals(cmd.src._o, cmd.o) &&  cmd.src._dt.l!=cmd.dt.l) {
        return;
      }

      int idx=_prOp.BinarySearch(cmd);
      if(idx<0) {
        idx=~idx;
        if(idx<=_pfPos) {
          cmd.art=Perform.Art.set;
          cmd.prim=sign;
          DoCmd(cmd);               // Published in next tick
          return;
        }
        _prOp.Insert(idx, cmd);
        cmd.src._json=null;
        cmd.old_vt=cmd.src._vt;
        cmd.old_dt=cmd.src._dt;
        cmd.old_o=cmd.src._o;
      } else {
        if(idx>=_pfPos) {
          cmd.art=Perform.Art.set;
          cmd.prim=sign;
          DoCmd(cmd);               // Published in next tick
          return;
        }
        var oCmd=_prOp[idx];
        if(oCmd.art==Perform.Art.changed) {
          cmd.old_vt=oCmd.old_vt;
          cmd.old_dt=oCmd.old_dt;
          cmd.old_o=oCmd.old_o;
        } else {
          cmd.src._json=null;
          cmd.old_vt=cmd.src._vt;
          cmd.old_dt=cmd.src._dt;
          cmd.old_o=cmd.src._o;
        }
        _prOp[idx]=cmd;
      }
      cmd.src._vt=cmd.vt;
      cmd.src._dt=cmd.dt;
      cmd.src._o=cmd.o;
    }
    internal void AddBlock(PiBlock bl) {
      _blocks.Add(bl);
    }
    internal PiVar GetVar(Topic t, bool create) {
      PiVar v;
      if(!_vars.TryGetValue(t, out v)) {
        if(create) {
          v=new PiVar(t);
          _vars[t]=v;
        } else {
          v=null;
        }
      }
      return v;
    }

  }
}
