﻿using NiL.JS;
using NiL.JS.Core;
using NiL.JS.Core.Modules;
using NiL.JS.Core.TypeProxing;
using JST = NiL.JS.BaseLibrary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X13.PLC {
  internal interface PlcItem : ITenant {
    void changed(Topic src, Perform p);
    int layer { get; }
  }

  internal class PiVar {
    public readonly Topic owner;

    public PlcItem _src;
    public bool ip { get { return _src!=null; } }
    public int layer;
    public PiBlock[] calcPath;
    public List<PlcItem> _cont;
    public PiBlock block { get; private set; }

    public PiVar(Topic src) {
      owner = src;
      _cont=new List<PlcItem>();
      layer=0;
      owner.Subscribe(owner_changed, SubRec.SubMask.Once, true);
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
      PiLink l;
      if((b= i as PiBlock)!=null) {
        block=b;
        if(block._decl!=null && block._decl.pins[owner.name].op) {
          if(ip) {
            throw new ArgumentException(string.Format("{0} already hat source {1}", owner.path, _src));
          }
          _src=i;
          owner.SetFlagI(0, false);
        }
      } else if((l=i as PiLink)!=null) {
        if(l.output==this) {
          if(ip) {
            throw new ArgumentException(string.Format("{0} already hat source {1}", owner.path, _src));
          }
          _src=i;
          owner.SetFlagI(0, false);
        }
      }
    }
    public void DelCont(PlcItem i) {
      _cont.Remove(i);
      if(block==i) {
        block=null;
      }
      if(i==_src) {
        owner.SetFlagI(0, true);
        _src=null;
      }
      if(_cont.Count==0) {
        owner.changed -= owner_changed;
        PLC.instance.DelVar(this);
      }
    }
    public override string ToString() {
      return string.Concat(owner.path, "[", this.ip ? "I" : " ", _cont.Count, ", ", layer.ToString(), "]");
    }
  }

  internal class PiAlias : CustomType, PlcItem {
    private Topic _owner;
    private List<PiLink> links;

    public bool ip;
    public bool op;
    private PiVar _origin;
    private Topic _tOrigin;

    public PiVar origin {
      get {
        if(_origin==null) {
          _origin=PLC.instance.GetVar(_tOrigin, true);
        }
        return _origin;
      }
    }
    public PiAlias(JSObject jso, Topic src, Topic prim)
      : this(src.GetI(jso["alias"].As<string>(), true, prim, true)) {
    }
    public PiAlias(Topic tOrigin) {
      _tOrigin=tOrigin;
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
        if(_owner==value) {
          return;
        }
        if(_owner!=null) {
          _owner.changed-=_owner_changed;
          origin.DelCont(this);
        }
        _owner=value;
        if(_owner!=null) {
          origin.AddCont(this);
          _owner.Subscribe(_owner_changed, SubRec.SubMask.Once, true);
        }
      }
    }
    [Hidden]
    public void changed(Topic src, Perform p) {
      if(src==origin.owner && p.art==Perform.Art.remove && _owner!=null) {
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

    public int layer {
      [Hidden]
      get { return origin.layer; }
    }
  }

  internal class PiLink : CustomType, PlcItem {
    internal static string RelativePath(Topic mp, Topic sp) {
      if(sp.path.Length>mp.path.Length && sp.path.StartsWith(mp.path) && sp.path[mp.path.Length]=='/') {
        return sp.path.Substring(mp.path.Length+1);
      }
      StringBuilder sb=new StringBuilder();
      var mpl=mp.path.Split(Topic.Bill.delmiterArr, StringSplitOptions.RemoveEmptyEntries);
      var spl=sp.path.Split(Topic.Bill.delmiterArr, StringSplitOptions.RemoveEmptyEntries);
      if(mpl.Length+1<spl.Length || mpl.Length>spl.Length+1) {
        return sp.path;
      }
      bool pass=true;
      int j=0;
      for(int i=0; i<mpl.Length; i++) {
        if(pass) {
          if(spl.Length<=i) {
            return sp.path;
          }
          if(mpl[i]!=spl[i]) {
            if(i+2<spl.Length) {
              return sp.path;
            }
            j=i;
            pass=false;
          } else {
            j=i+1;
            continue;
          }
        }
        sb.Append("../");
      }
      for(; j<spl.Length; j++) {
        sb.Append(spl[j]);
        if(j+1<spl.Length) {
          sb.Append("/");
        }
      }
      return sb.ToString();
    }

    private Topic _owner;
    private Topic _ipTopic;
    private Topic _opTopic;

    public PiVar input;
    public PiVar output;

    public PiLink(JSObject jso, Topic src, Topic prim)
      : this(src.parent.GetI(jso["i"].As<string>(), true, prim, true), src.parent.GetI(jso["o"].As<string>(), true, prim, true)) {
    }
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
          if((al = _opTopic.As<PiAlias>()) != null) {
            al.DelLink(this);
          }
          output.DelCont(this);
        }

        _owner = value;

        if(_owner != null) {
          if(_ipTopic.vType==typeof(PiAlias) && (al = _ipTopic.As<PiAlias>()) != null) {
            input = al.origin;
            al.AddLink(this);
            PLC.instance.GetVar(input.owner, true, true);
          } else {
            input = PLC.instance.GetVar(_ipTopic, true, true);
          }
          if(_opTopic.vType==typeof(PiAlias) && (al = _opTopic.As<PiAlias>()) != null) {
            output = al.origin;
            al.AddLink(this);
            PLC.instance.GetVar(output.owner, true, true);
          } else {
            output = PLC.instance.GetVar(_opTopic, true, true);
          }

          input.AddCont(this);
          output.AddCont(this);
          if(input.layer != 0 || input.owner._value.IsDefinded) {
            output.owner.SetI(input.owner._value, _owner);
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
      } else if(p.art == Perform.Art.changed || p.art==Perform.Art.subscribe) {
        if(src.vType == typeof(PiAlias)) {
          PiAlias al = src.As<PiAlias>();
          if(src == _ipTopic) {
            input.DelCont(this);
            input = al.origin;
            al.AddLink(this);
            input.AddCont(this);
          } else if(src == _opTopic) {
            if(al.origin.ip) {
              throw new ArgumentException(string.Format("{0} already hat source", _opTopic.path));
            }
            output.DelCont(this);
            output = al.origin;
            al.AddLink(this);
            output.AddCont(this);
          } else {
            return;
          }
        } else if(src != input.owner) {
          return;
        }
        if(input.layer!=0 || input.owner._value.IsDefinded) {
          output.owner._value=input.owner._value;
          if(src==input.owner) {
            var c=Perform.Create(output.owner, Perform.Art.changed, this.owner);
            c.o=input.owner._value;
            PLC.instance.DoCmd(c, true);
          }
        }
      }
    }
    [DoNotEnumerate]
    public JSObject toJSON(JSObject obj) {
      var r=JSObject.CreateObject();
      r["$type"]="PiLink";
      r["i"] = _owner==null || _owner.parent==null?_ipTopic.path:RelativePath(_owner.parent, _ipTopic);
      r["o"] = _owner==null || _owner.parent==null?_opTopic.path:RelativePath(_owner.parent, _opTopic);
      return r;
    }
    [Hidden]
    public override string ToString() {
      return string.Concat(_ipTopic.path, " >> ", _opTopic.path);
    }

    public int layer {
      [Hidden]
      get { 
        return (input==null || input.layer==0)?0:input.layer+1; 
      }
    }
  }

  internal class PiBlock : CustomType, PlcItem {
    static PiBlock() {
    }

    private Topic _owner;
    private string _funcName;
    internal PiDeclarer _decl;

    public PiBlock[] calcPath;
    public SortedList<string, PiVar> _pins;

    public PiBlock(JSObject jso, Topic src, Topic prim)
      : this(jso["func"].As<string>()) {
    }
    public PiBlock(string func) {
      _funcName=func;
      _pins = new SortedList<string, PiVar>();
      calcPath = new PiBlock[] { this };
    }

    private void children_changed(Topic src, Perform p) {
      if(_decl==null) {
        return;
      }
      if(p.art == Perform.Art.remove) {
        PiVar v;
        if(_pins.TryGetValue(src.name, out v)) {
          _pins.Remove(src.name);
          v.DelCont(this);
        }
      } else if(p.art != Perform.Art.unsubscribe) {
        PinDeclarer pd = AddPin(src);
        if(pd!=null && ((p.art == Perform.Art.changed && pd.ip) || p.art == Perform.Art.subscribe)) {
          _decl.Calc(this);
        }
      }
    }

    private PinDeclarer AddPin(Topic src) {
      PinDeclarer pd;
      if(!_decl.pins.TryGetValue(src.name, out pd)) {
        return null;
      }
      PiVar v;
      if(!_pins.TryGetValue(src.name, out v)) {
        v = PLC.instance.GetVar(src, true, true);
        v.AddCont(this);
        _pins.Add(src.name, v);
        if(_pins.Count == 1) {
          PLC.instance.AddBlock(this);
        }
      }
      return pd;
    }

    protected override JSObject GetMember(JSObject name, bool forWrite, bool own) {
      if(_owner == null) {
        return JSObject.Undefined;
      }
      if(name.As<string>()=="toJSON") {
        return base.GetMember(name, forWrite, own);
      }
      string pName=name.As<string>();
      if(_decl.pins.ContainsKey(pName)) {
        Topic r = _owner.GetI(pName, forWrite, _owner, true);
        if(r == null) {
          return JSObject.Undefined;
        }
        return r._value;
      } else {
        return base.GetMember(name, forWrite, own);
      }
    }
    protected override void SetMember(JSObject name, JSObject value, bool strict) {
      string pName=name.As<string>();
      if(_decl.pins.ContainsKey(pName)) {
        if(_owner == null) {
          return;
        }
        Topic r = _owner.GetI(name.ToString(), true, _owner, true);
        r.SetI(value.Clone(), _owner);
      } else {
        base.SetMember(name, value, strict);
      }
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
          _decl=PiDeclarer.Get(_funcName);
          if(_decl==null) {
            X13.lib.Log.Warning("{0}[{1}] declarer not found", _owner.path, _funcName);
          } else {
            foreach(var p in _decl.pins.Where(z => z.Value.mandatory)) {
              Topic t=_owner.GetI(p.Key, true, _owner, true);
              if(p.Value.defaultValue!=null && t.vType==null) {
                t.Set(p.Value.defaultValue, _owner);
              }
            }
            foreach(var t in _owner.children) {
              AddPin(t);
            }
            _owner.Subscribe(children_changed, SubRec.SubMask.Chldren, true);
          }
        }
      }
    }
    [Hidden]
    public void changed(Topic src, Perform p) {

    }
    public int layer {
      [Hidden]
      get;
      [Hidden]
      set;
    }

  }

  internal class PiDeclarer : CustomType {
    private static JST.Function ctor;
    static PiDeclarer() {
      ctor= new Script("function Construct(){ return Function.apply(null, arguments); }").Context.GetVariable("Construct").Value as JST.Function;
    }

    public static PiDeclarer Get(string name) {
      Topic t;
      if(Topic.root.Get("/etc/PLC/func", true).Exist(name, out t)) {
        return t.As<PiDeclarer>();
      }
      return null;
    }
    public static PiDeclarer Create(JSObject jso, Topic src, Topic prim) {
      PiDeclarer rez;
      if(src.vType==typeof(PiDeclarer)) {
        rez=src.As<PiDeclarer>();
      } else {
        rez=new PiDeclarer();
      }

      JSObject tmp;
      tmp=jso["init"];
      if(tmp.ValueType==JSObjectType.String) {
        rez._initFunc = ctor.Invoke(new Arguments { tmp }) as JST.Function;
      }
      tmp=jso["calc"];
      if(tmp.ValueType==JSObjectType.String) {
        rez._calcFunc = ctor.Invoke(new Arguments { tmp }) as JST.Function;
      }
      tmp=jso["deinit"];
      if(tmp.ValueType==JSObjectType.String) {
        rez._deinitFunc = ctor.Invoke(new Arguments { tmp }) as JST.Function;
      }
      rez.pins=new SortedList<string, PinDeclarer>();
      tmp=jso["pins"];
      if(tmp.ValueType==JSObjectType.Object) {
        foreach(var p in tmp) {
          rez.pins[p]=new PinDeclarer(tmp[p]);
        }
      }
      if((tmp=jso["info"]).ValueType==JSObjectType.String) {
        rez.info=tmp.As<string>();
      } else {
        rez.info=string.Empty;
      }
      if((tmp=jso["image"]).ValueType==JSObjectType.String) {
        rez.image=tmp.As<string>();
      } else {
        rez.image=null;
      }
      return rez;
    }

    private JST.Function _initFunc;
    private JST.Function _calcFunc;
    private JST.Function _deinitFunc;

    public string info;
    public string image;
    public SortedList<string, PinDeclarer> pins;

    private PiDeclarer() {
    }

    public void Init(PiBlock This) {
      if(_initFunc!=null) {
        _initFunc.Invoke(This, null);
      }
    }
    public void Calc(PiBlock This) {
      if(_calcFunc!=null) {
        _calcFunc.Invoke(This, null);
      }
    }
    public void DeInit(PiBlock This) {
      if(_deinitFunc!=null) {
        _deinitFunc.Invoke(This, null);
      }
    }

    [DoNotEnumerate]
    public JSObject toJSON(JSObject obj) {
      var r=JSObject.CreateObject();
      r["$type"]="PiDeclarer";
      if(_initFunc!=null) {
        r["init"]=GetFunctionBody(_initFunc);
      }
      if(_calcFunc!=null) {
        r["calc"]=GetFunctionBody(_calcFunc);
      }
      if(_deinitFunc!=null) {
        r["deinit"]=GetFunctionBody(_deinitFunc);
      }
      if(pins!=null && pins.Count>0) {
        var p=JSObject.CreateObject();
        foreach(var kv in pins) {
          p[kv.Key]=kv.Value.toJSON(null);
        }
        r["pins"]=p;
      }
      if(!string.IsNullOrEmpty(info)) {
        r["info"]=info;
      }
      if(image!=null) {
        r["image"]=image;
      }
      return r;
    }
    private string GetFunctionBody(JST.Function f) {
      string full=f.ToString();
      int bi=full.IndexOf('{');
      int ei=full.LastIndexOf('}');
      return full.Substring(bi+3, ei-bi-5);
    }
  }

  internal class PinDeclarer {
    public readonly int position;
    public readonly bool ip;
    public readonly bool op;
    public readonly bool mandatory;
    public readonly string info;
    public readonly string declarer;
    public readonly JSObject defaultValue;

    public PinDeclarer(JSObject jso) {
      JSObject tmp;
      if((tmp=jso["pos"]).ValueType==JSObjectType.String) {
        string ps=tmp.As<string>();
        if(ps==null || ps.Length!=1) {
          position=-1;
        } else if(ps[0]>='A' && ps[0]<='Z') {  // inputs
          ip=true;
          op=false;
          position=(int)(ps[0]-'A');
        } else if(ps[0]>='a' && ps[0]<='z') {  // outputs
          ip=false;
          op=true;
          position=(int)(ps[0]-'a');
        } else if(ps[0]>='0' && ps[0]<='9') {  // parameters
          ip=false;
          op=false;
          position=(int)(ps[0]-'0');
        } else {
          position=-1;
        }
      } else {
        position=-1;
      }
      if((tmp=jso["mandatory"]).ValueType==JSObjectType.Bool && tmp.As<bool>()) {
        mandatory=true;
      } else {
        mandatory=false;
      }
      if((tmp=jso["info"]).ValueType==JSObjectType.String) {
        info=tmp.As<string>();
      } else {
        info=string.Empty;
      }
      if((tmp=jso["declarer"]).ValueType==JSObjectType.String) {
        declarer=tmp.As<string>();
      } else {
        declarer=string.Empty;
      }
      if((tmp=jso["default"]).IsExist) {
        defaultValue=tmp;
      } else {
        defaultValue=null;
      }
    }
    [DoNotEnumerate]
    public JSObject toJSON(JSObject obj) {
      var r=JSObject.CreateObject();
      if(ip) {
        r["pos"]=(char)('A'+position);
      } else if(op) {
        r["pos"]=(char)('a'+position);
      } else {
        r["pos"]=(char)('0'+position);
      }
      if(mandatory) {
        r["mandatory"]=true;
      }
      if(!string.IsNullOrEmpty(info)) {
        r["info"]=info;
      }
      if(!string.IsNullOrEmpty(declarer)) {
        r["declarer"]=info;
      }
      if(defaultValue!=null) {
        r["default"]=defaultValue;
      }
      return r;
    }
  }
}