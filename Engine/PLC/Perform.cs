using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X13.PLC {
  public class Perform: IComparable<Perform> {
    private static int[] _prio;
    private static Dictionary<Type, Delegate> _getters;

    static Perform() {
      //create=1,         1
      //subscribe=2,      2  
      //unsubscribe=3,    2
      //set=4,            2
      //changed=5,        2
      //move=6,           3
      //remove=7          3
      _prio=new int[] { 0, 1, 2, 2, 2, 2, 3, 3 };

      _getters=new Dictionary<Type, Delegate>();
      _getters.Add(typeof(bool), (Delegate)new Func<Topic.VT, object, Topic.PriDT, bool>(GetBool));
      _getters.Add(typeof(long), (Delegate)new Func<Topic.VT, object, Topic.PriDT, long>(GetLong));
      _getters.Add(typeof(double), (Delegate)new Func<Topic.VT, object, Topic.PriDT, double>(GetDouble));
      _getters.Add(typeof(DateTime), (Delegate)new Func<Topic.VT, object, Topic.PriDT, DateTime>(GetDateTime));
      _getters.Add(typeof(string), (Delegate)new Func<Topic.VT, object, Topic.PriDT, string>(GetString));
    }
    private static bool GetBool(Topic.VT vt, object o, Topic.PriDT dt) {
      bool ret;
      switch(vt) {
      case Topic.VT.Bool:
      case Topic.VT.Integer:
      case Topic.VT.DateTime:
        ret=dt.l!=0;
        break;
      case Topic.VT.Float:
        ret=dt.d!=0;
        break;
      case Topic.VT.String:
        if(!bool.TryParse((string)o, out ret)) {
          ret=false;
        }
        break;
      default:
        ret=false;
        break;
      }
      return ret;
    }
    private static long GetLong(Topic.VT vt, object o, Topic.PriDT dt) {
      long ret;
      switch(vt) {
      case Topic.VT.Bool:
      case Topic.VT.Integer:
      case Topic.VT.DateTime:
        ret=dt.l;
        break;
      case Topic.VT.Float:
        ret=(long)Math.Truncate(dt.d);
        break;
      case Topic.VT.String:
        if(!long.TryParse((string)o, out ret)) {
          double tmp;
          if(double.TryParse((string)o, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out tmp)) {
            ret=(long)Math.Truncate(tmp);
          } else {
            ret=0;
          }
        }
        break;
      default:
        ret=0;
        break;
      }
      return ret;
    }
    private static double GetDouble(Topic.VT vt, object o, Topic.PriDT dt) {
      double ret;
      switch(vt) {
      case Topic.VT.Bool:
      case Topic.VT.Integer:
        ret=dt.l;
        break;
      case Topic.VT.Float:
        ret=dt.d;
        break;
      case Topic.VT.DateTime:
        ret=dt.dt.ToOADate();
        break;
      case Topic.VT.String:
        if(!double.TryParse((string)o, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ret)) {
          ret=0;
        }
        break;
      default:
        ret=0;
        break;
      }
      return ret;
    }
    private static DateTime GetDateTime(Topic.VT vt, object o, Topic.PriDT dt) {
      DateTime ret;
      switch(vt) {
      case Topic.VT.DateTime:
        ret=dt.dt;
        break;
      case Topic.VT.Bool:
      case Topic.VT.Integer:
        ret=new DateTime(dt.l);
        break;
      case Topic.VT.Float:
        ret=DateTime.FromOADate(dt.d);
        break;
      case Topic.VT.String:
        DateTime.TryParse((string)o, out ret);
        break;
      default:
        ret=DateTime.MinValue;
        break;
      }
      return ret;
    }
    private static string GetString(Topic.VT vt, object o, Topic.PriDT dt) {
      string ret;
      switch(vt) {
      case Topic.VT.Bool:
        ret=dt.l==0?bool.FalseString:bool.TrueString;
        break;
      case Topic.VT.Integer:
        ret=dt.l.ToString();
        break;
      case Topic.VT.DateTime:
        ret=dt.dt.ToString();
        break;
      case Topic.VT.Float:
        ret=dt.d.ToString();
        break;
      case Topic.VT.String:
        ret=(string)o;
        break;
      case Topic.VT.Object:
        ret=o==null?string.Empty:o.ToString();
        break;
      default:
        ret=string.Empty;
        break;
      }
      return ret;
    }
    internal static T GetVal<T>(Topic.VT vt, ref object o, Topic.PriDT dt) {
      if(vt==Topic.VT.Ref && typeof(T)!=typeof(Topic)) {
        Topic r=o as Topic;
        return r==null?default(T):r.As<T>();
      }
      if(vt==Topic.VT.Null) {
        return default(T);
      }
      try {
        Delegate d;
        if(_getters.TryGetValue(typeof(T), out d)) {
          Func<Topic.VT, object, Topic.PriDT, T> f;
          if((f=d as Func<Topic.VT, object, Topic.PriDT, T>)!=null) {
            return f(vt, o, dt);
          }
        }
        if(o==null) {
          switch(vt) {
          case Topic.VT.Bool:
            o=dt.l!=0;
            break;
          case Topic.VT.Integer:
            o=dt.l;
            break;
          case Topic.VT.DateTime:
            o=dt.dt;
            break;
          case Topic.VT.Float:
            o=dt.d;
            break;
          }
        }
        return (T)o;
      }
      catch(Exception) {
      }
      return default(T);
    }

    internal static void Set<T>(T val, ref Topic.VT vt, ref object o, ref Topic.PriDT dt) {
      try {
        if(vt==Topic.VT.Ref && typeof(T)!=typeof(Topic)) {
          (o as Topic).Set<T>(val); // ????
          return;
        }
        o=val;
        switch(Type.GetTypeCode(o==null?null:o.GetType())) {
        case TypeCode.Boolean:
          dt.l=(bool)o?1:0;
          vt=Topic.VT.Bool;
          break;
        case TypeCode.Byte:
        case TypeCode.SByte:
        case TypeCode.Int16:
        case TypeCode.Int32:
        case TypeCode.Int64:
        case TypeCode.UInt16:
        case TypeCode.UInt32:
        case TypeCode.UInt64:
          dt.l=Convert.ToInt64(o);
          vt=Topic.VT.Integer;
          break;
        case TypeCode.Single:
        case TypeCode.Double:
        case TypeCode.Decimal:
          dt.d=Convert.ToDouble(o);
          vt=Topic.VT.Float;
          break;
        case TypeCode.DateTime:
          dt.dt=(DateTime)o;
          vt=Topic.VT.DateTime;
          break;
        case TypeCode.Empty:
          dt.l=0;
          vt=Topic.VT.Null;
          break;
        case TypeCode.Object:
        default:
          if(val is Topic) {
            vt=Topic.VT.Ref;
          } else if(val is string) {
            vt=Topic.VT.String;
          } else {
            vt=Topic.VT.Object;
          }
          break;
        }
      }
      catch(Exception) {
      }
    }

    internal static Perform Create<T>(Topic src, T val, Topic prim) {
      Perform r;
      if(typeof(T)==typeof(Art)) {
        r=new Perform((Art)(object)val, src, prim);
        r.vt=Topic.VT.Undefined;
        r.o=null;
        r.dt.l=0;
      } else {
        r=new Perform(Art.set, src, prim);
        Set(val, ref r.vt, ref r.o, ref r.dt);
      }
      return r;
    }
    internal Topic.VT vt;
    internal Topic.PriDT dt;
    internal object o;
    internal Topic.VT old_vt;
    internal Topic.PriDT old_dt;
    internal object old_o;

    public readonly Topic src;
    public Topic prim { get; internal set; }
    public readonly int layer;
    public Art art { get; internal set; }

    private Perform(Art art, Topic src, Topic prim) {
      this.src=src;
      this.art=art;
      this.prim=prim;

      PiBlock b;
      PiVar v;
      if(vt==Topic.VT.Object && (b=o as PiBlock)!=null) {
        this.layer=b.layer;
      } else if((v=PLC.instance.GetVar(src, false))!=null) {
        this.layer=v.layer;
      } else {
        this.layer=int.MinValue;
      }
    }

    public int CompareTo(Perform other) {
      if(other==null) {
        return -1;
      }
      if(this.layer!=other.layer) {
        return this.layer.CompareTo(other.layer);
      }
      if(this.src.path!=other.src.path) {
        return this.src.path.CompareTo(other.src.path);
      }
      return _prio[((int)this.art)].CompareTo(_prio[(int)(other.art)]);
    }

    public enum Art {
      create=1,
      subscribe=2,
      unsubscribe=3,
      set=4,
      changed=5,
      move=6,
      remove=7
    }
  }
}
