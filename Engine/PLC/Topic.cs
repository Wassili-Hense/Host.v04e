using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using X13.lib;

namespace X13.PLC {
  public sealed class Topic: IComparable<Topic> {
    public static readonly Topic root;

    static Topic() {
      root=new Topic(null, "/");
    }
    #region variables
    internal SortedList<string, Topic> _children;
    internal List<SubRec> _subRecords;
    internal Topic _parent;
    internal string _name;
    internal string _path;
    /// <summary>[0] - saved, [1] - local, [2] - disposed, [3] - disposed fin., [4] - config </summary>
    internal System.Collections.BitArray _flags;

    internal VT _vt;
    internal PriDT _dt;
    internal object _o;
    internal string _json;
    #endregion variables

    private Topic(Topic parent, string name) {
      _flags=new System.Collections.BitArray(5);
      _flags[0]=true;  // saved
      _name=name;
      _parent=parent;
      _vt=VT.Undefined;
      _dt.l=0;
      _o=null;

      if(parent==null) {
        _path="/";
      } else if(parent==root) {
        _path="/"+name;
      } else {
        _path=parent.path+"/"+name;
        _flags[1]=parent.local;
      }
    }

    public Topic parent {
      get { return _parent; }
    }
    public string name {
      get { return _name; }
    }
    public string path {
      get { return _path; }
    }
    public Type vType {
      get {
        switch(_vt) {
        case VT.Null:
        case VT.Undefined:
          return null;
        case VT.Bool:
          return typeof(bool);
        case VT.Integer:
          return typeof(long);
        case VT.Float:
          return typeof(double);
        case VT.DateTime:
          return typeof(DateTime);
        case VT.String:
          return _o==null?null:typeof(string);
        case VT.Ref:
          return _o==null?null:typeof(Topic);
        case VT.Object:
          return _o==null?null:_o.GetType();
        }
        return null;
      }
    }
    public Bill all { get { return new Bill(this, true); } }
    public Bill children { get { return new Bill(this, false); } }
    /// <summary>save value in persistent storage</summary>
    public bool saved {
      get { return _flags[0]; }
      set {
        if(_flags[0]!=value) {
          _flags[0]=value;
          var c=Perform.Create(this, Perform.Art.changed, null);
          PLC.instance.DoCmd(c);
        }
      }
    }
    /// <summary>only for this instance</summary>
    public bool local { get { return _flags[1]; } set { _flags[1]=value; } }
    /// <summary>removed</summary>
    public bool disposed { get { return _flags[2]; } }
    /// <summary>save value only in config file</summary>
    public bool config {
      get { return _flags[4]; }
      set {
        if(_flags[4]!=value) {
          _flags[4]=value;
          var c=Perform.Create(this, Perform.Art.changed, null);
          PLC.instance.DoCmd(c);
        }
      }
    }

    /// <summary> Get item from tree</summary>
    /// <param name="path">relative or absolute path</param>
    /// <param name="create">true - create, false - check</param>
    /// <returns>item or null</returns>
    public Topic Get(string path, bool create=true, Topic prim=null) {
      if(string.IsNullOrEmpty(path)) {
        return this;
      }
      Topic home=this, next;
      if(path[0]==Bill.delmiter) {
        if(path.StartsWith(this._path)) {
          path=path.Substring(this._path.Length);
        } else {
          home=Topic.root;
        }
      }
      var pt=path.Split(Bill.delmiterArr, StringSplitOptions.RemoveEmptyEntries);
      for(int i=0; i<pt.Length; i++) {
        if(pt[i]==Bill.maskAll || pt[i]==Bill.maskChildren) {
          throw new ArgumentException(string.Format("{0}[{1}] dont allow wildcard", this.path, path));
        }
        next=null;
        if(home._children==null) {
          home._children=new SortedList<string, Topic>();
        } else if(home._children.TryGetValue(pt[i], out next)) {
          home=next;
        }
        if(next==null) {
          if(create) {
            next=new Topic(home, pt[i]);
            home._children.Add(pt[i], next);
            var c=Perform.Create(next, Perform.Art.create, prim);
            PLC.instance.DoCmd(c);
          } else {
            return null;
          }
        }
        home=next;
      }
      return home;
    }
    public bool Exist(string path) {
      return Get(path, false)!=null;
    }
    public bool Exist(string path, out Topic topic) {
      return (topic=Get(path, false))!=null;
    }
    public void Remove(Topic prim=null) {
      var c=Perform.Create(this, Perform.Art.remove, prim);
      PLC.instance.DoCmd(c);
    }
    public Topic Move(Topic nParent, string nName, Topic prim=null) {
      if(nParent==null) {
        nParent=this.parent;
      }
      if(string.IsNullOrEmpty(nName)) {
        nName=this.name;
      }
      if(nParent.Exist(nName)) {
        throw new ArgumentException(string.Concat(this.path, ".Move(", nParent.path, "/", nName, ") - destination already exist"));
      }
      Topic dst=new Topic(nParent, nName);
      nParent._children.Add(nName, dst);
      var c=Perform.Create(this, Perform.Art.move, prim);
      c.o=dst;
      PLC.instance.DoCmd(c);
      return dst;
    }
    public override string ToString() {
      return _path;
    }
    public int CompareTo(Topic other) {
      if(other==null) {
        return 1;
      }
      return string.Compare(this._path, other._path);
    }

    public void Set<T>(T val, Topic prim=null) {
      if(_vt==VT.Ref && typeof(T)!=typeof(Topic) ) {
        (_o as Topic).Set(val, prim);
      } else {
        var c=Perform.Create(this, val, prim);
        PLC.instance.DoCmd(c);
      }
    }
    public T As<T>() {
      return Perform.GetVal<T>(_vt, ref _o, _dt);
    }
    
    public void SetJson(string json, Topic prim=null) {
      var c=Perform.Create(this, Perform.Art.set, prim);
      c.o=json;
      c.vt=VT.Json;
      PLC.instance.DoCmd(c);
    }
    public string ToJson() {
      if(_json==null) {
        lock(this) {
          if(_json==null) {
            switch(_vt) {
            case VT.Null:
              _json=Jurassic.Null.NullString;
              break;
            case VT.Undefined:
              _json=Jurassic.Undefined.UndefinedString;
              break;
            case VT.Bool:
              _json=_dt.l==0?Jurassic.Library.BooleanConstructor.FalseString:Jurassic.Library.BooleanConstructor.TrueString;
              break;
            case VT.Integer:
              _json=_dt.l.ToString();
              break;
            case VT.Float:
              if(double.IsInfinity(_dt.d) || double.IsNaN(_dt.d)){
                _json=Jurassic.Null.NullString;
              }else{
                _json=_dt.d.ToString(System.Globalization.CultureInfo.InvariantCulture);
                int idx;
                if((idx=_json.LastIndexOf('E'))>=0){
                  if(_json[idx+1]=='0') {
                    _json=_json.Remove(idx+1, 1);
                  } else if(_json.Length>idx+2 && (_json[idx+1]=='-' || _json[idx+1]=='+') && _json[idx+2]=='0') {
                    _json=_json.Remove(idx+2, 1);
                  }
                } else if(_json.LastIndexOf('.')<0) {
                    _json=_json+".0";
                }
              }
              break;
            case VT.DateTime:
              _json=_dt.dt.ToUniversalTime().ToString(@"""'""yyyy-MM-dd'T'HH:mm:ss.fff""Z'""", System.Globalization.DateTimeFormatInfo.InvariantInfo);
              break;
            case VT.String: {
              StringBuilder sb=new StringBuilder();
              Jurassic.Library.JSONSerializer.QuoteString(_o as string, sb);
              _json=sb.ToString();
              }
              break;
            case VT.Ref: {
                StringBuilder sb=new StringBuilder();
                sb.Append("{\"$ref\":");
                Jurassic.Library.JSONSerializer.QuoteString((_o as Topic).path, sb);
                sb.Append("}");
                _json=sb.ToString();
              }
              break;
            default:
              _json=Jurassic.Library.JSONObject.Stringify(PLC.instance.Engine, _o);
              break;
            }
          }
        }
      }
      return _json;
    }
    
    public event Action<Topic, Perform> changed {
      add {
        var c=Perform.Create(this, Perform.Art.subscribe, this);
        c.o=value;
        _dt.l=0;
        PLC.instance.DoCmd(c);
      }
      remove {
        var c=Perform.Create(this, Perform.Art.unsubscribe, this);
        c.o=value;
        _dt.l=0;
        PLC.instance.DoCmd(c);
      }
    }

    internal void Publish(Perform cmd) {
      Action<Topic, Perform> func;
      if(cmd.art==Perform.Art.subscribe && (func=cmd.o as Action<Topic, Perform>)!=null) {
        try {
          func(this, cmd);
        }
        catch(Exception ex) {
          Log.Warning("{0}.{1}({2}, {4}) - {3}", func.Method.DeclaringType.Name, func.Method.Name, this.path, ex.ToString(), cmd.art.ToString());
        }
      } else {
        if(_subRecords!=null) {
          for(int i=0; i<_subRecords.Count; i++) {
            if((func=_subRecords[i].f)!=null && (_subRecords[i].ma.Length==0 || _subRecords[i].ma[0]==Bill.maskAll)) {
              try {
                func(this, cmd);
              }
              catch(Exception ex) {
                Log.Warning("{0}.{1}({2}, {4}) - {3}", func.Method.DeclaringType.Name, func.Method.Name, this.path, ex.ToString(), cmd.art.ToString());
              }
            }
          }
        }
      }
    }
    internal void RefChanged(Topic t, Perform c) {
      if(_vt==VT.Ref && (_o as Topic)==c.src) {
        if(c.art==Perform.Art.move) {
          Topic dst;
          if((dst=c.o as Topic)!=null) {
            _o=dst;
            dst.Subscribe(new SubRec() { f=this.RefChanged, mask=dst.path, ma=Bill.curArr });
            var cmd=Perform.Create(this, dst, c.prim);
            cmd.art=Perform.Art.changed;
            this.Publish(cmd);
          }
        } else if(c.art==Perform.Art.changed) {
          this.Publish(c);
        } else if(c.art==Perform.Art.remove) {
          var cmd=Perform.Create(this, Perform.Art.changed, c.prim);
          cmd.old_vt=_vt;
          _vt=c.old_vt;
          cmd.vt=_vt;
          cmd.old_dt=_dt;
          _dt=c.old_dt;
          cmd.dt=_dt;
          cmd.old_o=_o;
          _o=c.old_o;
          cmd.o=_o;
          this.Publish(cmd);
        }
      }
    }
    internal void Subscribe(SubRec sr) {
      if(this._subRecords==null) {
        this._subRecords=new List<SubRec>();
      }
      if(!this._subRecords.Exists(z => z.f==sr.f && z.mask==sr.mask)) {
        this._subRecords.Add(sr);
      }
    }
    internal void Unsubscribe(string mask, Action<Topic, Perform> f) {
      if(this._subRecords!=null) {
        this._subRecords.RemoveAll(z => z.f==f && z.mask==mask);
      }
    }

    #region nested types
    public class Bill: IEnumerable<Topic> {
      public const char delmiter='/';
      public const string delmiterStr="/";
      public const string maskAll="#";
      public const string maskChildren="+";
      public static readonly char[] delmiterArr=new char[] { delmiter };
      public static readonly string[] curArr=new string[0];
      public static readonly string[] allArr=new string[] { maskAll };
      public static readonly string[] childrenArr=new string[] { maskChildren };

      private Topic _home;
      private bool _deep;

      public Bill(Topic home, bool deep) {
        _home=home;
        _deep=deep;
      }
      public IEnumerator<Topic> GetEnumerator() {
        if(!_deep) {
          if(_home._children!=null) {
            Topic[] ch=_home._children.Values.ToArray();
            for(int i=ch.Length-1; i>=0; i--) {
              yield return ch[i];
            }
          }
        } else {
          var hist=new Stack<Topic>();
          Topic[] ch;
          Topic cur;
          hist.Push(_home);
          do {
            cur=hist.Pop();
            yield return cur;
            if(cur._children!=null) {
              ch=cur._children.Values.ToArray();
              for(int i=ch.Length-1; i>=0; i--) {
                hist.Push(ch[i]);
              }
            }
          } while(hist.Any());
        }
      }
      public event Action<Topic, Perform> changed {
        add {
          Perform c=Perform.Create(_home, Perform.Art.subscribe, _home);
          c.o=value;
          c.dt.l=_deep?2:1;
          PLC.instance.DoCmd(c);
        }
        remove {
          Perform c=Perform.Create(_home, Perform.Art.unsubscribe, _home);
          c.o=value;
          c.dt.l=_deep?2:1;
          PLC.instance.DoCmd(c);
        }
      }
      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return GetEnumerator();
      }
    }

    internal enum VT {
      Undefined = 0,
      Null,
      Bool,
      Integer,
      Float,
      DateTime,
      String,
      Object,
      //Array,
      //Record,
      //Binary,
      Ref,
      Json,
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct PriDT {
      [FieldOffset(0)]
      public Int64 l;
      [FieldOffset(0)]
      public double d;
      [FieldOffset(0)]
      public DateTime dt;
    }

    internal struct SubRec {
      public string mask;
      public string[] ma;
      public Action<Topic, Perform> f;
    }
    #endregion nested types
  }

  public interface ITenant {
    Topic owner { get; set; }
  }
}
