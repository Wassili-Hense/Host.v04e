using NiL.JS.Core;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using X13.lib;

namespace X13.PLC {
  public sealed class Topic : IComparable<Topic> {
    public static readonly Topic root;

    static Topic() {
      root=new Topic(null, "/");
    }
    #region variables
    private Topic _parent;
    private string _name;
    private string _path;
    /// <summary>[0] - saved, [1] - local, [2] - disposed, [3] - disposed fin., [4] - config </summary>
    private BitArray _flags;
    internal ConcurrentDictionary<string, Topic> _children;
    internal List<SubRec> _subRecords;
    internal string _json;
    internal JSObject _value;
    #endregion variables

    private Topic(Topic parent, string name) {
      _flags=new System.Collections.BitArray(5);
      _flags[0]=true;  // saved
      _name=name;
      _parent=parent;
      _value=JSObject.Undefined;

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
      internal set { _parent=value; }
    }
    public string name {
      get { return _name; }
    }
    public string path {
      get { return _path; }
      internal set { _path=value; }
    }
    public Type vType {
      get {
        switch(_value.ValueType) {
        case JSObjectType.NotExists:
        case JSObjectType.NotExistsInObject:
        case JSObjectType.Undefined:
          return null;
        case JSObjectType.Bool:
          return typeof(bool);
        case JSObjectType.Int:
          return typeof(long);
        case JSObjectType.Double:
          return typeof(double);
        case JSObjectType.Date:
          return typeof(DateTime);
        case JSObjectType.String:
          return typeof(string);
        case JSObjectType.Object:
          return _value.Value.GetType();
        }
        return null;
      }
    }
    public Bill all { get { return new Bill(this, true); } }
    public Bill children { get { return new Bill(this, false); } }
    /// <summary>save defaultValue in persistent storage</summary>
    public bool saved {
      get { return _flags[0]; }
      set {
        if(_flags[0]!=value) {
          _flags[0]=value;
          var c=Perform.Create(this, Perform.Art.changed, null);
          PLC.instance.DoCmd(c, false);
        }
      }
    }
    /// <summary>only for this instance</summary>
    public bool local { get { return _flags[1]; } set { _flags[1]=value; } }
    /// <summary>removed</summary>
    public bool disposed { get { return _flags[2]; } internal set { _flags[2]=value; } }
    /// <summary>save defaultValue only in config file</summary>
    public bool config {
      get { return _flags[4]; }
      set {
        if(_flags[4]!=value) {
          _flags[4]=value;
          var c=Perform.Create(this, Perform.Art.changed, null);
          PLC.instance.DoCmd(c, false);
        }
      }
    }

    /// <summary> Get item from tree</summary>
    /// <param name="path">relative or absolute path</param>
    /// <param name="create">true - create, false - check</param>
    /// <returns>item or null</returns>
    public Topic Get(string path, bool create=true, Topic prim=null) {
      return GetI(path, create, prim, false);
    }
    internal Topic GetI(string path, bool create, Topic prim, bool inter) {
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
        if(pt[i]==Bill.maskParent) {
          home=home.parent;
          if(home==null) {
            throw new ArgumentException(string.Format("{0}[{1}] BAD path: excessive nesting", this.path, path));
          }
          continue;
        }
        next=null;
        if(home._children==null) {
          lock(home) {
            if(home._children==null) {
              home._children=new ConcurrentDictionary<string, Topic>();
            }
          }
        } else if(home._children.TryGetValue(pt[i], out next)) {
          home=next;
        }
        if(next==null) {
          if(create) {
            if(home._children.TryGetValue(pt[i], out next)) {
              home=next;
            } else {
              next=new Topic(home, pt[i]);
              home._children[pt[i]]=next;
              var c=Perform.Create(next, Perform.Art.create, prim);
              PLC.instance.DoCmd(c, inter);
            }
          } else {
            return null;
          }
        }
        home=next;
      }
      return home;
    }
    internal void SetFlagI(int fl, bool value) {
      _flags[fl]=value;
    }
    public bool Exist(string path) {
      return GetI(path, false, null, false)!=null;
    }
    public bool Exist(string path, out Topic topic) {
      return (topic=GetI(path, false, null, false))!=null;
    }
    public void Remove(Topic prim=null) {
      var c=Perform.Create(this, Perform.Art.remove, prim);
      PLC.instance.DoCmd(c, false);
    }
    public Topic Move(Topic nParent, string nName, Topic prim=null) {
      if(nParent==null) {
        nParent=this.parent;
      }
      if(string.IsNullOrEmpty(nName)) {
        nName=this.name;
      }
      //if(nParent.Exist(nName)) {
      //  throw new ArgumentException(string.Concat(this.path, ".Move(", nParent.path, "/", nName, ") - destination already exist"));
      //}
      Topic dst=new Topic(nParent, nName);
      lock(nParent._children) {
        nParent._children[nName]=dst;
      }
      var c=Perform.Create(this, Perform.Art.move, prim);
      c.o=dst;
      PLC.instance.DoCmd(c, false);
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

    public object value { get { return (_value.ValueType>=JSObjectType.Object && !(_value.Value is JSObject))?_value.Value:_value; } set { this.Set(value); } }
    public T As<T>() {
      try {
        return _value.As<T>();
      }
      catch(Exception) {

      }
      return default(T);
    }
    public void Set(object val, Topic prim=null) {
      var c=Perform.Create(this, val, prim);
      PLC.instance.DoCmd(c, false);
    }
    internal void SetI(object val, Topic prim=null) {
      var c=Perform.Create(this, val, prim);
      PLC.instance.DoCmd(c, true);
    }
    public void SetJson(string json, Topic prim=null) {
      var c=Perform.Create(this, Perform.Art.setJson, prim);
      c.o=json;
      PLC.instance.DoCmd(c, false);
    }
    internal void SetIJson(string json, Topic prim=null) {
      var c=Perform.Create(this, Perform.Art.setJson, prim);
      c.o=json;
      PLC.instance.DoCmd(c, true);
    }

    public string ToJson() {
      if(_json==null) {
        lock(this) {
          if(_json==null) {
            var t=_value.ValueType;
            if(t==JSObjectType.NotExists || t==JSObjectType.NotExistsInObject || t==JSObjectType.Undefined) {
              _json="null";
            } else {
              _json=NiL.JS.Core.Modules.JSON.stringify(_value, null, null);
            }
          }
        }
      }
      return _json;
    }

    public event Action<Topic, Perform> changed {
      add {
        Subscribe(value, false);
      }
      remove {
        var c=Perform.Create(this, Perform.Art.unsubscribe, this);
        c.o=value;
        c.i=0;
        PLC.instance.DoCmd(c, false);
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
    internal void Subscribe(SubRec sr) {
      if(this._subRecords==null) {
        this._subRecords=new List<SubRec>();
      }
      if(!this._subRecords.Exists(z => z.f==sr.f && z.mask==sr.mask)) {
        this._subRecords.Add(sr);
      }
    }
    internal void Subscribe(Action<Topic, Perform> func, bool intern) {
      var c=Perform.Create(this, Perform.Art.subscribe, this);
      c.o=func;
      c.i=0;
      PLC.instance.DoCmd(c, intern);
    }
    internal void Unsubscribe(string mask, Action<Topic, Perform> f) {
      if(this._subRecords!=null) {
        this._subRecords.RemoveAll(z => z.f==f && z.mask==mask);
      }
    }

    #region nested types
    public class Bill : IEnumerable<Topic> {
      public const char delmiter='/';
      public const string delmiterStr="/";
      public const string maskAll="#";
      public const string maskChildren="+";
      public const string maskParent="..";
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
            foreach(var t in _home._children.OrderBy(z=>z.Key)) {
              yield return t.Value;
            }
          }
          yield break;
        } else {
          var hist=new Stack<Topic>();
          Topic cur;
          hist.Push(_home);
          do {
            cur=hist.Pop();
            yield return cur;
            if(cur._children!=null) {
              foreach(var t in cur._children.OrderByDescending(z=>z.Key)) {
                hist.Push(t.Value);
              }
            }
          } while(hist.Any());
        }
      }
      public event Action<Topic, Perform> changed {
        add {
          Subsribe(value, false);
        }
        remove {
          Perform c=Perform.Create(_home, Perform.Art.unsubscribe, _home);
          c.o=value;
          c.i=_deep?2:1;
          PLC.instance.DoCmd(c, false);
        }
      }
      internal void Subsribe(Action<Topic, Perform> f, bool intern) {
        Perform c=Perform.Create(_home, Perform.Art.subscribe, _home);
        c.o=f;
        c.i=_deep?2:1;
        PLC.instance.DoCmd(c, intern);
      }
      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return GetEnumerator();
      }
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
