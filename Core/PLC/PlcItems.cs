using NiL.JS.Core;
using NiL.JS.Core.Modules;
using NiL.JS.Core.TypeProxing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X13.PLC {
  internal class PiVar {
    public Topic owner;

    public bool ip;
    public bool op;
    public int layer;
    public PiBlock[] calcPath;
    public PiBlock block;
    public List<PiLink> links;

    public PiVar(Topic src) {
      this.owner = src;
      links=new List<PiLink>();
      layer=0;
    }
    public void AddLink(PiLink l) {
      links.Add(l);
    }
    public override string ToString() {
      return string.Concat(owner.path, "[", this.ip?"I":" ", this.op?"O":" ", ", ", layer.ToString(), "]");
    }
  }
  public class PiAlias : CustomType, ITenant {
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

  public class PiLink : CustomType, ITenant {
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
      set { _owner=value; }
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
  }
  public class PiBlock : CustomType, ITenant, IComparable<PiBlock> {
    private Topic _owner;
    internal int layer;
    internal PiBlock[] calcPath;
    internal SortedList<string, PiVar> _pins;

    public Topic owner {
      get { return _owner; }
      set { _owner=value; }
    }

    public int CompareTo(PiBlock other) {
      int l1=this.layer<=0?(this._pins.Select(z => z.Value).Where(z1 => z1.ip && z1.layer>0).Max(z2 => z2.layer)):this.layer;
      int l2=other==null?int.MaxValue:(other.layer<=0?(other._pins.Select(z => z.Value).Where(z1 => z1.ip && z1.layer>0).Max(z2 => z2.layer)):other.layer);
      return l1.CompareTo(l2);
    }
  }
}