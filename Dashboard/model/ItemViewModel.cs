using NiL.JS.Core;
using NiL.JS.Core.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace X13.model {
  internal class ItemViewModel : ViewModelBase {
    private static char[] _delmiter=new char[] { '/' };
    public static readonly ItemViewModel root;

    static ItemViewModel() {
      root=new ItemViewModel(null, "/") { posX=0, posY=0, sizeX=25, sizeY=20, view=Projection.IN };
    }

    private string _name;
    private ObservableCollection<ItemViewModel> _children;
    private ItemViewModel _parent;
    private ValueVM _value;

    private ItemViewModel(ItemViewModel parent, string name) {
      _name=name;
      _parent=parent;
      _value=new ValueVM(this, _name, null);
    }

    public IEnumerable<ItemViewModel> children {
      get {
        if(_children==null) {
          _children=new ObservableCollection<ItemViewModel>();
          _children.Add(new ItemViewModel(this, "Alpha") { posX=5, posY=3, sizeX=25, sizeY=20, view=Projection.IN});
          _children.Add(new ItemViewModel(this, "Beta") { posX=15, posY=3, sizeX=25, sizeY=20, view=Projection.IN });
          if(_name!="Delta") {
            _children.Add(new ItemViewModel(this, "Gamma") { posX=5, posY=12, sizeX=25, sizeY=20, view=Projection.IN });
          }
          _children.Add(new ItemViewModel(this, "Delta") { posX=15, posY=12, sizeX=25, sizeY=20, view=Projection.IN });
        }
        return _children;
      }
    }
    public IEnumerable<ValueVM> Properties {
      get {
        return _value.Properties;
      }
    }
    public string Name { get { return _name; } }
    public ValueVM ValueO { get { return _value; } }
    public object Value { get { return _value.Value; } set { _value.Value=value; } }
    public string path { get { return _parent==null?"/":(_parent==root?"/"+_name:_parent.path+"/"+_name); } }
    public string contentId { get { return view.ToString()+":"+path; } }
    public Projection view { get; set; }
    public int posX { get; set; }
    public int posY { get; set; }
    public int sizeX { get; set; }
    public int sizeY { get; set; }

    internal ItemViewModel Get(string p) {
      ItemViewModel cur;
      ItemViewModel next=null;
      if(!string.IsNullOrEmpty(p) && p.StartsWith("/")) {
        cur=root;
      } else {
        cur=this;
      }
      if(string.IsNullOrEmpty(p)) {
        return cur;
      }
      string[] pe=p.Split(_delmiter, StringSplitOptions.RemoveEmptyEntries);
      for(int i=0; i<pe.Length; i++, cur=next) {
        next=cur.children.FirstOrDefault(z => z._name==pe[i]);    // create & fill if null
        bool chExist=next!=null;
        if(!chExist) {
          lock(cur) {
            next=cur._children.FirstOrDefault(z => z._name==pe[i]);
            chExist=next!=null;
            if(!chExist) {
              if(pe[i]=="+" || pe[i]=="#") {
                throw new ArgumentException("path ("+path+") is not valid");
              }
              next=new ItemViewModel(cur, pe[i]);
              cur._children.Add(next);
            }
          }
        }
      }
      return cur;
    }
    internal void Update() {
      X13.lib.Log.Debug("{0}={1}", _name, _value.ToString());
    }
    public override string ToString() {
      return _value.ToString();
    }
  }
  public enum Projection {
    /// <summary>InspectorView</summary>
    IN,
    /// <summary>LogramView</summary>
    LO,
    /// <summary>SourceView</summary>
    SO
  }
}
