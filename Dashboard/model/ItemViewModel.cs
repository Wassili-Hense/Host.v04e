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
    private static Action<string, string, string> _re2;
    public static readonly ItemViewModel root;

    static ItemViewModel() {
      root=new ItemViewModel(null, "/") { posX=0, posY=0, sizeX=25, sizeY=20, view=Projection.IN };
      WsClient.instance.Event+=RcvEvent;
      _re2=new Action<string, string, string>(RcvEvent2);
    }

    static void RcvEvent(string path, string payload, string options) {
      if(System.Windows.Application.Current!=null) {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(_re2, path, payload, options);
      }
    }
    static void RcvEvent2(string path, string payload, string options) {
      var t=root.Get(path);
      t._value=new ValueVM(t, JSON.parse(payload, null));
      t._value.PropertyChanged+=t._value_PropertyChanged;
      t.RaisePropertyChanged("Value");
      t.RaisePropertyChanged("Properties");
      t.RaisePropertyChanged("ViewType");
    }

    void _value_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
      this.RaisePropertyChanged(e.PropertyName);  
    }

    private string _name;
    private ObservableCollection<ItemViewModel> _children;
    private ItemViewModel _parent;
    private ValueVM _value;

    private ItemViewModel(ItemViewModel parent, string name) {
      _name=name;
      _parent=parent;
      _value=new ValueVM(this, null);
    }

    public IEnumerable<ItemViewModel> children {
      get {
        if(_children==null) {
          _children=new ObservableCollection<ItemViewModel>();
          WsClient.instance.Subscribe(this.path, 2);    // /path/+
        }
        return _children;
      }
    }
    public IEnumerable<ValueVM> Properties {
      get {
        return _value.Properties;
      }
    }
    public string ViewType { get { return _value.ViewType; } set { _value.ViewType=value; } }
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
      //X13.lib.Log.Debug("{0}={1}", _name, _value.ToString());
      WsClient.instance.Publish(this.path, JSON.stringify(_value._value, null, null));
    }
    internal void Remove() {
      if(_parent!=null) {
        _parent._children.Remove(this);
        WsClient.instance.Publish(this.path, string.Empty);
      }
    }
    public override string ToString() {
      return _value==null?"null":_value.ToString();
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
