using NiL.JS.Core.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace X13.model {
  internal class TopicM : PropertyM {
    #region static
    private static char[] _delmiter=new char[] { '/' };
    private static Action<string, string, string> _re2;
    public static readonly TopicM root;

    static TopicM() {
      root=new TopicM(null, WsClient.instance.Info);
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
      t.SetValue(JSON.parse(payload, null));
    }
    #endregion static

    private ObservableCollection<TopicM> _children;

    private TopicM(TopicM parent, string name)
      : base(parent, name) {
    }

    public ObservableCollection<TopicM> Children {
      get {
        if(_children==null) {
          _children=new ObservableCollection<TopicM>();
          WsClient.instance.Subscribe(this.Path, 2);    // /path/+
        }
        return _children;
      }
    }

    public string Path { get { return _parent==null?"/":(_parent==root?"/"+Name:(_parent as TopicM).Path+"/"+Name); } }
    public string ContentId { get { return View.ToString()+":"+Path; } }
    public Projection View { get; set; }
    public IEnumerable<TopicM> NameList { get { return _parent==null?(new TopicM[] { this }):(_parent as TopicM).NameList.Union(new TopicM[] { this }); } }

    public int sizeX { get; set; }
    public int sizeY { get; set; }
    public double posX { get; set; }
    public double posY { get; set; }

    public TopicM Get(string p) {
      TopicM cur;
      TopicM next=null;
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
        next=cur.Children.Cast<TopicM>().FirstOrDefault(z => z.Name==pe[i]);    // create & fill if null
        bool chExist=next!=null;
        if(!chExist) {
          lock(cur) {
            next=cur.Children.Cast<TopicM>().FirstOrDefault(z => z.Name==pe[i]);
            chExist=next!=null;
            if(!chExist) {
              if(pe[i]=="+" || pe[i]=="#") {
                throw new ArgumentException("path ("+Path+") is not valid");
              }
              next=new TopicM(cur, pe[i]);
              int idx;
              for(idx=0; idx<cur._children.Count; idx++) {
                if(!cur._children[idx].EditName && string.Compare(cur._children[idx].Name, pe[i])>0) {
                  break;
                }
              }
              cur.Children.Insert(idx, next);
            }
          }
        }
      }
      return cur;
    }
    public void AddChild() {
      Children.Insert(0, new TopicM(this, string.Empty));
    }
    public override void SetName(string nname) {
      if(string.IsNullOrEmpty(Name)) {   // Create
        Name=nname;
        WsClient.instance.Create(this.Path, "undefined");
        this.Remove();
      } else {    // rename
        //TODO: rename
      }
    }
    protected override void Publish() {
      WsClient.instance.Publish(this.Path, JSON.stringify(_value, null, null));
    }
    public override void Remove() {
      var p=_parent as TopicM;
      if(p!=null) {
        p._children.Remove(this);
        if(!EditName) {
          WsClient.instance.Publish(this.Path, string.Empty);
        }
      }
    }

    public override string ToString() {
      return Path;
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
