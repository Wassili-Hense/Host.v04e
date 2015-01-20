using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X13.PLC {
  public class Perform: IComparable<Perform> {

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
      int p1=((int)this.art)>>2;
      int p2=(int)(other.art)>>2;
      if(p1!=p2) {
        return p1.CompareTo(p2);
      }
      return this.layer>other.layer?1:-1;
    }
    public override string ToString() {
      return string.Concat(src.path, "[", art.ToString(), ", ", layer.ToString() , "]=", o==null?"null":o.ToString());
    }
    public enum Art {
      create=1,
      set=4,
      setJson=5,
      changed=6,
      subscribe=8,
      unsubscribe=12,
      move=16,
      remove=17
    }
  }
}
