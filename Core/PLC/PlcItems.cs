using NiL.JS;
using NiL.JS.Core;
using NiL.JS.Core.Modules;
using NiL.JS.Core.TypeProxing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X13.PLC {
  internal interface PlcItem : ITenant {
    void changed(Topic src, Perform p);
  }

  internal class PiVar {
    public readonly Topic owner;

    public bool ip;
    public bool op;
    public int layer;
    public PiBlock[] calcPath;
    public List<PlcItem> _cont;

    public PiVar(Topic src) {
      owner = src;
      _cont=new List<PlcItem>();
      layer=0;
      owner.changed+=owner_changed;
    }

    private void owner_changed(Topic src, Perform p) {
      for(int i=_cont.Count-1; i>=0; i--) {
        _cont[i].changed(src, p);
      }
      if(p.art==Perform.Art.remove) {
        PLC.instance.DelVar(this);
      }
    }
    public void AddCont(PlcItem i) {
      _cont.Add(i);
      PiBlock b;
      if((b= i as PiBlock)!=null) {
        if(block!=null) {
          block=b;
        }
      }
    }
    public void DelCont(PlcItem i) {
      _cont.Remove(i);
      if(block==i) {
        block=null;
      }
      if(!_cont.Any(z => z is PiLink && (z as PiLink).output==this)) {
        ip=false;
      }
      if(_cont.Count==0) {
        owner.changed -= owner_changed;
        PLC.instance.DelVar(this);
      }
    }
    public PiBlock block { get; private set; }
    public override string ToString() {
      return string.Concat(owner.path, "[", this.ip ? "I" : " ", this.op ? "O" : " ", ", ", layer.ToString(), "]");
    }
  }
  internal class PiAlias : CustomType, PlcItem {
    private Topic _owner;
    private List<PiLink> links;

    public bool ip;
    public bool op;
    public PiVar origin;

    public PiAlias(Topic tOrigin) {
      origin=PLC.instance.GetVar(tOrigin, true);
      links=new List<PiLink>();
    }
    [Hidden]
    public void AddLink(PiLink l) {
      links.Add(l);
    }
    [Hidden]
    public void DelLink(PiLink l) {
      links.Remove(l);
    }

    [DoNotEnumerate]
    public JSObject toJSON(JSObject obj) {
      var r=JSObject.CreateObject();
      r["$type"]="PiAlias";
      r["alias"]=origin.owner.path;
      return r;
    }

    public Topic owner {
      [Hidden]
      get {
        return _owner;
      }
      [Hidden]
      set {
        if(_owner!=null) {
          _owner.changed-=_owner_changed;
        }
        _owner=value;
        if(_owner == null) {
          if(_owner!=null) {
            _owner.changed-=_owner_changed;
          }
        }
      }
    }
    [Hidden]
    public void changed(Topic src, Perform p) {
      if(p.art==Perform.Art.remove && _owner!=null) {
        _owner.Remove(p.prim);
      }
    }
    private void _owner_changed(Topic snd, Perform p) {
      if(p.art==Perform.Art.remove) {
        for(int i=links.Count-1; i>=0; i--) {
          links[i].owner.Remove(p.prim);
        }
      }
    }
  }

  internal class PiLink : CustomType, PlcItem {
    private Topic _owner;
    private Topic _ipTopic;
    private Topic _opTopic;
    public PiVar input;
    public PiVar output;
    public int layer;

    public PiLink(Topic ip, Topic op) {
      _ipTopic = ip;
      _opTopic = op;
    }
    [Hidden]
    public Topic owner {
      [Hidden]
      get { return _owner; }
      [Hidden]
      set {
        if(_owner == value) {
          return;
        }
        PiAlias al;
        if(_owner != null) {
          if((al = _ipTopic.As<PiAlias>()) != null) {
            al.DelLink(this);
          }
          input.DelCont(this);
          if((al = _ipTopic.As<PiAlias>()) != null) {
            al.DelLink(this);
          }
          output.DelCont(this);
        }

        _owner = value;

        if(_owner != null) {
          if((al = _ipTopic.As<PiAlias>()) != null) {
            input = al.origin;
            al.AddLink(this);
          } else {
            input = PLC.instance.GetVar(_ipTopic, true);
          }
          input.op = true;
          if((al = _opTopic.As<PiAlias>()) != null) {
            output = al.origin;
            al.AddLink(this);
          } else {
            output = PLC.instance.GetVar(_opTopic, true);
          }
          if(output.ip) {
            throw new ArgumentException(string.Format("{0} already hat source", _opTopic.path));
          }
          output.ip = true;

          input.AddCont(this);
          output.AddCont(this);
          if(input.layer != 0 || input.owner._value.IsDefinded) {
            output.owner.Set(input.owner._value, _owner);
          }
        }
      }
    }
    [Hidden]
    public void changed(Topic src, Perform p) {
      if(_owner == null) {
        return;
      }
      if(p.art == Perform.Art.remove) {
        _owner.Remove(p.prim);
      } else if(p.art == Perform.Art.changed) {
        if(src.vType == typeof(PiAlias)) {
          PiAlias al = src.As<PiAlias>();
          if(src == _ipTopic) {
            input.DelCont(this);
            input = al.origin;
            input.op = true;
            al.AddLink(this);
            input.AddCont(this);
          } else if(src == _opTopic) {
            output.DelCont(this);
            output = al.origin;
            if(output.ip) {
              throw new ArgumentException(string.Format("{0} already hat source", _opTopic.path));
            }
            output.ip = true;
            al.AddLink(this);
            output.AddCont(this);
          } else {
            return;
          }
        } else if(src != input.owner) {
          return;
        }
        if(input.layer!=0 || input.owner._value.IsDefinded) {
          output.owner.Set(input.owner._value, _owner);
        }
      }
    }
    [DoNotEnumerate]
    public JSObject toJSON(JSObject obj) {
      var r=JSObject.CreateObject();
      r["$type"]="PiLink";
      r["i"] = _ipTopic.path;
      r["o"] = _opTopic.path;
      return r;
    }
    [Hidden]
    public override string ToString() {
      return string.Concat(input.owner.path, " - ", output.owner.path);
    }
  }
  internal class PiBlock : CustomType, PlcItem, IComparable<PiBlock> {
    private static NiL.JS.Core.BaseTypes.Function ctor;
    static PiBlock() {
      ctor= new Script("function Construct(){ return Function.apply(null, arguments); }").Context.GetVariable("Construct").Value as NiL.JS.Core.BaseTypes.Function;
    }

    private Topic _owner;
    private string _funcName;
    private NiL.JS.Core.BaseTypes.Function _calcFunc;

    public int layer;
    public PiBlock[] calcPath;
    public SortedList<string, PiVar> _pins;

    public PiBlock(string func) {
      _funcName=func;
      string body="this.Q=this.A + 1;";
      _calcFunc = ctor.Invoke(new Arguments { body }) as NiL.JS.Core.BaseTypes.Function;
      _pins = new SortedList<string, PiVar>();
      calcPath = new PiBlock[] { this };
    }

    private void children_changed(Topic src, Perform p) {
      if(src.name == "$INF") {
        return;
      }
      if(p.art == Perform.Art.create || p.art == Perform.Art.subscribe) {
        if(!_pins.ContainsKey(src.name)) {
          var pin = PLC.instance.GetVar(src, true, true);
          if(src.name=="A") {
            //pin.ip=true;
          } else if(src.name == "Q") {
            pin.op = true;
          }
          pin.AddCont(this);
          _pins.Add(src.name, pin);
          if(_pins.Count == 1) {
            PLC.instance.AddBlock(this);
          }
        }
      }
      if(p.art == Perform.Art.changed || p.art == Perform.Art.subscribe) {
        PiVar v;
        if(_pins.TryGetValue(src.name, out v) && (v.ip || p.art == Perform.Art.subscribe) && p.prim != PLC.instance.sign) {
          Calculate();
        }
      }
    }
    private void Calculate() {
      _calcFunc.Invoke(this, null);
    }
    protected override JSObject GetMember(JSObject name, bool forWrite, bool own) {
      if(_owner == null) {
        return JSObject.Undefined;
      }
      if(name.As<string>()=="toJSON") {
        return base.GetMember(name, forWrite, own);
      }
      Topic r = _owner.Get(name.As<string>(), forWrite, _owner);
      if(r == null) {
        return JSObject.Undefined;
      }
      return r._value;
    }
    protected override void SetMember(JSObject name, JSObject value, bool strict) {
      if(_owner == null) {
        return;
      }
      Topic r = _owner.Get(name.ToString(), true, _owner);
      r.Set(value, _owner);
    }
    protected override IEnumerator<string> GetEnumeratorImpl(bool pdef) {
      return _pins.OrderBy(z => z.Key).OrderBy(z => z.Value.layer).Select(z => z.Key).GetEnumerator();
    }
    [DoNotEnumerate]
    public JSObject toJSON(JSObject obj) {
      var r=JSObject.CreateObject();
      r["$type"]="PiBlock";
      r["func"]=_funcName;
      return r;
    }
    [Hidden]
    public override string ToString() {
      return string.Concat(_owner==null?string.Empty:_owner.path, "[", _funcName, ", ", layer.ToString(), "]");
    }
    [Hidden]
    public int CompareTo(PiBlock other) {
      int l1=this.layer<=0?(this._pins.Select(z => z.Value).Where(z1 => z1.ip && z1.layer>0).Max(z2 => z2.layer)):this.layer;
      int l2=other==null?int.MaxValue:(other.layer<=0?(other._pins.Select(z => z.Value).Where(z1 => z1.ip && z1.layer>0).Max(z2 => z2.layer)):other.layer);
      return l1.CompareTo(l2);
    }

    public Topic owner {
      [Hidden]
      get { return _owner; }
      [Hidden]
      set {
        if(_owner == value) {
          return;
        }
        if(_owner != null) {
          _owner.children.changed -= children_changed;
        }
        _owner=value;
        if(_owner != null) {
          _owner.children.changed += children_changed;
        }
      }
    }
    [Hidden]
    public void changed(Topic src, Perform p) {

    }
  }
}