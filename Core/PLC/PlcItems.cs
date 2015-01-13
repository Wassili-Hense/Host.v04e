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
  public interface PlcItem : ITenant {
  }

  internal class PiVar : IDisposable {
    public readonly Topic owner;

    public bool ip;
    public bool op;
    public int layer;
    public PiBlock[] calcPath;
    public PiBlock block;
    public List<PiLink> links;

    public PiVar(Topic src) {
      owner = src;
      links=new List<PiLink>();
      layer=0;
      owner.changed+=owner_changed;
    }

    private void owner_changed(Topic src, Perform p) {
      if(p.art==Perform.Art.remove) {
        for(int i=links.Count-1; i>=0; i--) {
          links[i].Del();
        }
        PLC.instance.DelVar(this);
      } else if(p.art==Perform.Art.changed) {
        for(int i=links.Count-1; i>=0; i--) {
          if(links[i].input==this) {
            links[i].output.owner.Set(this.owner._value, links[i].owner);
          }
        }
      }
    }
    public void AddLink(PiLink l) {
      links.Add(l);
    }
    internal void DelLink(PiLink l) {
      links.Remove(l);
    }
    public override string ToString() {
      return string.Concat(owner.path, "[", this.ip?"I":" ", this.op?"O":" ", ", ", layer.ToString(), "]");
    }

    public void Dispose() {

    }
  }
  public class PiAlias : CustomType, PlcItem {
    private Topic _owner;
    public bool ip;
    public bool op;
    internal PiVar alias;
    internal List<PiLink> links;
    public Topic owner {
      get { return _owner; }
      set { _owner=value; }
    }
    public void AddLink(PiLink l) {
      links.Add(l);
      alias.links.Add(l);
    }
  }

  public class PiLink : CustomType, PlcItem {
    private Topic _owner;
    internal PiVar input;
    internal PiVar output;
    internal int layer;

    public PiLink(Topic ip, Topic op)
      : this(PLC.instance.GetVar(ip, true), PLC.instance.GetVar(op, true)) {
    }
    internal PiLink(PiVar ip, PiVar op) {
      base.ValueType=JSObjectType.Object;

      input=ip;
      input.op=true;
      input.AddLink(this);
      if(op.ip) {
        throw new ArgumentException(string.Format("{0} already hat source", op.owner.path));
      }
      output=op;
      output.ip=true;
      output.AddLink(this);
    }
    [Hidden]
    public Topic ip {
      [Hidden]
      get { return input.owner; } 
    }
    [Hidden]
    public Topic op {
      [Hidden]
      get { return output.owner; } 
    }
    [Hidden]
    public Topic owner {
      [Hidden]
      get { return _owner; }
      [Hidden]
      set {
        _owner=value;
        if(_owner == null) {
          input.DelLink(this);
          output.DelLink(this);
        }
      }
    }

    [DoNotEnumerate]
    public JSObject toJSON(JSObject obj) {
      var r=JSObject.CreateObject();
      r["i"]=input.owner.path;
      r["o"]=output.owner.path;
      return r;
      //return string.Concat("{ \"i\" : ", System.Web.HttpUtility.JavaScriptStringEncode(input.owner.path, true),
      //  ", \"o\" : ", System.Web.HttpUtility.JavaScriptStringEncode(output.owner.path, true), " }");
    }
    [Hidden]
    public override string ToString() {
      return string.Concat(input.owner.path," - ", output.owner.path);
    }

    internal void Del() {
      input.DelLink(this);
      output.DelLink(this);
      owner.Remove(owner);
    }
  }
  public class PiBlock : CustomType, PlcItem, IComparable<PiBlock> {
    private static NiL.JS.Core.BaseTypes.Function ctor;
    static PiBlock() {
      ctor= new Script("function Construct(){ return Function.apply(null, arguments); }").Context.GetVariable("Construct").Value as NiL.JS.Core.BaseTypes.Function;
    }

    private Topic _owner;
    internal int layer;
    internal PiBlock[] calcPath;
    internal SortedList<string, PiVar> _pins;
    private NiL.JS.Core.BaseTypes.Function _calcFunc;

    public PiBlock(string proto) {
      string body="this.Q=this.A + 1;";
      _calcFunc = ctor.Invoke(new Arguments { body }) as NiL.JS.Core.BaseTypes.Function;
      _pins = new SortedList<string, PiVar>();
      calcPath = new PiBlock[] { this };
    }
    public Topic owner {
      get { return _owner; }
      set {
        if (_owner == value) {
          return;
        }
        if (_owner != null) {
        }
        _owner=value;
        if (_owner != null) {
          _owner.children.changed += children_changed;
        }
      }
    }

    private void children_changed(Topic src, Perform p) {
      if (src.name == "$INF") {
        return;
      }
      if (p.art == Perform.Art.create || p.art == Perform.Art.subscribe) {
        if (!_pins.ContainsKey(src.name)) {
          var pin = PLC.instance.GetVar(src, true);
          if (src.name == "Q") {
            pin.op = true;
          }
          pin.block = this;
          _pins.Add(src.name, pin);
          if (_pins.Count == 1) {
            PLC.instance.AddBlock(this);
          }
        }
      }
      if (p.art == Perform.Art.changed || p.art == Perform.Art.subscribe) {
        PiVar v;
        if (_pins.TryGetValue(src.name, out v) && (v.ip || p.art == Perform.Art.subscribe) && p.prim != PLC.instance.sign) {
          Calculate();
        }
      }
    }
    private void Calculate() {
      _calcFunc.Invoke(this, null);
    }
    protected override JSObject GetMember(JSObject name, bool forWrite, bool own) {
      if (_owner == null) {
        return JSObject.Undefined;
      }
      Topic r = _owner.Get(name.ToString(), forWrite, _owner);
      if (r == null) {
        return JSObject.Undefined;
      }
      return r._value;
    }
    protected override void SetMember(JSObject name, JSObject value, bool strict) {
      if (_owner == null) {
        return;
      }
      Topic r = _owner.Get(name.ToString(), true, _owner);
      r.Set(value, _owner);
    }

    public int CompareTo(PiBlock other) {
      int l1=this.layer<=0?(this._pins.Select(z => z.Value).Where(z1 => z1.ip && z1.layer>0).Max(z2 => z2.layer)):this.layer;
      int l2=other==null?int.MaxValue:(other.layer<=0?(other._pins.Select(z => z.Value).Where(z1 => z1.ip && z1.layer>0).Max(z2 => z2.layer)):other.layer);
      return l1.CompareTo(l2);
    }
  }
}