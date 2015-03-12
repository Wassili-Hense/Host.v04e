using NiL.JS.Core.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace X13.model {
  internal class TopicM : PropertyM, IDisposable {
    #region static
    private static char[] _delmiter=new char[] { '/' };
    private static System.Windows.Threading.DispatcherTimer _ipqTimer;
    public static readonly TopicM root;

    static TopicM() {
      root=new TopicM(null, WsClient.instance.Info);
      _ipqTimer=new System.Windows.Threading.DispatcherTimer(new TimeSpan(900000), System.Windows.Threading.DispatcherPriority.Background, ProcMsgs, System.Windows.Application.Current.Dispatcher);
    }
    private static void ProcMsgs(object sender, EventArgs e) {
      Tuple<string, string, string> msg;
      while(WsClient.instance.Poll(out msg)) {
        if(string.IsNullOrEmpty(msg.Item2)) {
          var t=root.Get(msg.Item1, false);
          if(t!=null) {
            t.Remove(false);
          }
        } else {
          var t=root.Get(msg.Item1);
          t.SetValue(JSON.parse(msg.Item2, null));
        }
      }
    }
    #endregion static

    private ObservableCollection<TopicM> _children;
    private int _subscribed;  // TODO: subscribed parent/+ | self

    private TopicM(TopicM parent, string name)
      : base(parent, name) {
        if(parent!=null && (parent._subscribed & 2)==0) {
          WsClient.instance.Subscribe(this.Path, 1);
          _subscribed|=1;
        }
    }

    public ObservableCollection<TopicM> Children {
      get {
        if(_children==null) {
          _children=new ObservableCollection<TopicM>();
        }
        if((_subscribed&2)==0){
          WsClient.instance.Subscribe(this.Path, 2);    // path/+
          _subscribed|=2;
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

    public TopicM Get(string p, bool create=true) {
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
        if(cur._children==null) {
          next=null;
        } else {
          next=cur._children.Cast<TopicM>().FirstOrDefault(z => z.Name==pe[i]);    // create & fill if null
        }
        bool chExist=next!=null;
        if(!chExist) {
          if(create) {
            if(cur._children==null) {
              cur._children=new ObservableCollection<TopicM>();
              next=null;
            } else {
              next=cur._children.Cast<TopicM>().FirstOrDefault(z => z.Name==pe[i]);
            }

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
              cur._children.Insert(idx, next);
            }
          } else {
            return null;
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
        this.Remove(false);
      } else {    // rename
        //TODO: rename
      }
    }
    protected override void Publish() {
      string json=JSON.stringify(_value, null, null);
      if(string.IsNullOrEmpty(json)) {
        json="null";
      }
      WsClient.instance.Publish(this.Path, json);
    }
    public override void Remove(bool ext) {
      if(this!=root) {
        if(ext) {
          WsClient.instance.Publish(this.Path, string.Empty);
        } else {
          if(_children!=null) {
            for(int i=_children.Count-1; i>=0; i--) {
              _children[i].Remove(ext);
            }
          }
          (_parent as TopicM)._children.Remove(this);
          Workspace.This.CloseFile(this);
        }
      }
    }
    public override string ToString() {
      return Path;
    }

    public void Dispose() {
      WsClient.instance.Unsubscribe(this.Path, _subscribed); // path/+
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
