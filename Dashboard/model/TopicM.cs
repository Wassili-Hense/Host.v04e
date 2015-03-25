using NiL.JS.Core.Modules;
using JST = NiL.JS.BaseLibrary;
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
      root=new TopicM(null, WsClient.instance.Info);  // TODO: Для каждого клиента свой root
      _ipqTimer=new System.Windows.Threading.DispatcherTimer(new TimeSpan(900000), System.Windows.Threading.DispatcherPriority.Background, ProcMsgs, System.Windows.Application.Current.Dispatcher);
    }
    private static void ProcMsgs(object sender, EventArgs e) {
      JST.Array msg;
      int len;
      while(WsClient.instance.Poll(out msg)) {
        int cmd=msg["0"].As<int>();
        len=msg.length.As<int>();
        switch(cmd) {
        case 36: {
            string path=msg["1"].As<string>();
            if(string.IsNullOrEmpty(path)) {
              break;
            }
            if(len>2) {     // [Event, "path", value, options]
              var t=root.Get(path);
              t.SetValue(msg["2"]);
            } else if(len==2) {  //[Event, "path"] - remove
              var t=root.Get(path, false);
              if(t!=null) {
                t.Remove(false);
              }
            }
          }
          break;
        case 296:
          if(len==4){
            string opath=msg["1"].As<string>();
            string npath=msg["2"].As<string>();
            string nname=msg["3"].As<string>();
            TopicM ot, np;
            if(!string.IsNullOrEmpty(opath) && !string.IsNullOrEmpty(nname) && (ot=root.Get(opath, false))!=null) {
              np=ot._parent as TopicM;
              if(string.IsNullOrEmpty(npath) || npath==np.Path) {
                int j=-1-np.IndexOf(ot.Name);
                if(j<0) {
                  X13.lib.Log.Warning("Move({0}, {1}, {2}) - source not found", opath, npath, nname);
                  break;
                }
                int i=np.IndexOf(nname);
                if(i==j) {  // position is not changed
                  // do nothing
                } else if(i>=0) {
                  np._children.Move(j, i);
                } else {    // name already exist
                  i=-1-i;
                  np._children[i].Remove(false);
                  np._children.Move(i<j?j-1:j, i>j?i-1:i);
                }
              } else {
                (ot._parent as TopicM)._children.Remove(ot);
                np=root.Get(npath, true);
                ot._parent=np;
                int i=np.IndexOf(nname);
                if(i>=0) {
                  np._children.Insert(i, ot);
                } else {
                  np._children[-1-i]=ot;
                }
              }
              ot.Name=nname;
              ot.RaisePropertyChanged("Name");
              ot.RaisePropertyChanged("Path");
              ot.RaisePropertyChanged("ContentId");
              ot.RaisePropertyChanged("NameList");
            }
          }
          break;
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
      if(parent==null) {
        IsRoot=true;
      }
    }

    public ObservableCollection<TopicM> Children {
      get {
        if(_children==null) {
          _children=new ObservableCollection<TopicM>();
          this.RaisePropertyChanged("Children");
        }
        if((_subscribed&2)==0) {
          foreach(var t in _children.Where(z=>(z._subscribed&1)==1) ) {
            t._subscribed&=~1;
            WsClient.instance.Unsubscribe(t.Path, 1);
          }
          WsClient.instance.Subscribe(this.Path, 2);    // path/+
          _subscribed|=2;
        }
        return _children;
      }
    }

    public string Path { get { return _parent==null?"/":(_parent==root?"/"+Name:(_parent as TopicM).Path+"/"+Name); } }
    public TopicM Parent { get { return _parent as TopicM; } }
    public string ContentId { get { return View.ToString()+":"+Path; } }
    public Projection View { get; set; }
    public IEnumerable<TopicM> NameList { get { return _parent==null?(new TopicM[] { this }):(_parent as TopicM).NameList.Union(new TopicM[] { this }); } }

    public bool IsRoot { get; private set; }
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
            if((cur._subscribed&2)==0) {
              foreach(var t in _children.Where(z => (z._subscribed&1)==1)) {
                t._subscribed&=~1;
                WsClient.instance.Unsubscribe(t.Path, 1);
              }
              WsClient.instance.Subscribe(this.Path, 2, true);    // path/+  , wait SubAck
              _subscribed|=2;

            }
            chExist=next!=null;
            if(!chExist) {
              if(pe[i]=="+" || pe[i]=="#") {
                throw new ArgumentException("path ("+Path+") is not valid");
              }
              next=new TopicM(cur, pe[i]);
              //X13.lib.Log.Debug("{0}.Get({1}) - new({2})", this.Path, p, next.Path);
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
      if(!EditName) {
        return;
      }
      if(nname==null) {
        nname=this.Name;
      }
      if(string.IsNullOrEmpty(Name)) {
        if(string.IsNullOrEmpty(nname)) {  // create, chancel
          this.Remove(false);
        } else {      // create
          Name=nname;
          WsClient.instance.Publish(this.Path, "null");
          this.Remove(false);
        }
      } else {  // rename
        if(nname==Name) {  // rename, chancel

        } else {          // rename
          WsClient.instance.Move(this.Path, (_parent as TopicM).Path, nname);
        }
      }
      EditName=false;
      RaisePropertyChanged("EditName");
      RaisePropertyChanged("Name");
      RaisePropertyChanged("Path");
      RaisePropertyChanged("ContentId");
      RaisePropertyChanged("NameList");
    }
    protected override void Publish() {
      string json=JST.JSON.stringify(_value, null, null);
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
    public override string GetUri(string p) {
      StringBuilder sb=new StringBuilder();
      if(IsRoot){
        sb.Append("x13://");
      } else {
        sb.Append(_parent.GetUri(null));
        sb.Append("/");
      }
      sb.Append(Name);

      if(p!=null) {
        sb.Append("?");
        sb.Append(p);
      }
      return sb.ToString();
    }

    public void Dispose() {
      WsClient.instance.Unsubscribe(this.Path, _subscribed); // path/+
    }

    private int IndexOf(string name) {
      int i, j;
      if(_children==null) {
        _children=new ObservableCollection<TopicM>();
      }
      for(i=_children.Count-1; i>=0; i--) {
        j=string.Compare(_children[i].Name, name);
        if(j==0) {
          i=-1-i;
          break;
        }
        if(j<0) {
          break;
        }
      }
      return i;
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
