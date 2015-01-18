using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X13.PLC {
  public class Perform: IComparable<Perform> {
    private static int[] _prio;

    static Perform() {
      //create=1,         1
      //subscribe=2,      2  
      //unsubscribe=3,    2
      //set=4,            2
      //setJson=5,        2
      //changed=6,        2
      //move=7,           3
      //remove=8          3
      _prio=new int[] { 0, 1, 2, 2, 2, 2, 2, 3, 3 };

    }
    internal static Perform Create(Topic src, object val, Topic prim) {
      Perform r;
      if(val!=null && val.GetType()==typeof(Art)) {
        r=new Perform((Art)(object)val, src, prim);
        r.o=null;
        r.i=0;
      } else {
        r=new Perform(Art.set, src, prim);
        r.o=val;
        r.i=0;
      }
      return r;
    }
    internal object o;
    internal int i;
    internal object old_o;

    public readonly Topic src;
    public Topic prim { get; internal set; }
    public readonly int layer;
    public Art art { get; internal set; }

    private Perform(Art art, Topic src, Topic prim) {
      this.src=src;
      this.art=art;
      this.prim=prim;

      PiVar v;
      if((v=PLC.instance.GetVar(src, false))!=null) {
        this.layer=v.layer;
      } else {
        this.layer=-1;
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
    public override string ToString() {
      return string.Concat(src.path, "[", art.ToString(), ", ", layer.ToString() , "]=", o==null?"null":o.ToString());
    }
    public enum Art {
      create=1,
      subscribe=2,
      unsubscribe=3,
      set=4,
      setJson=5,
      changed=6,
      move=7,
      remove=8
    }
  }
}
