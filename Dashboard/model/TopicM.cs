using NiL.JS.Core.Modules;
using JST = NiL.JS.BaseLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace X13.model {
  internal class TopicM : PropertyM, IDisposable {
    #region static
    private static char[] _delmiter=new char[] { '/' };

    public static TopicM CreateRoot(WsClient cl) {
      TopicM root=new TopicM(null, cl.Info, cl);
      return root;
    }
    #endregion static

    private ObservableTopics _children;
    public int _subscribed;
    public readonly WsClient _client;

    private TopicM(TopicM parent, string name, WsClient cl=null)
      : base(parent, name) {
      if(parent==null) {
        IsRoot=true;
        _client=cl;
      } else {
        _client=parent._client;
      }
    }

    public ObservableCollection<TopicM> Children {
      get {
        bool chChanged=false;
        if(_children==null) {
          _children=new ObservableTopics();
          chChanged=true;
        }
        if((_subscribed&2)==0) {
          X13.lib.Log.Debug("{0}.Children", this.Path);
          _client.Subscribe(this==_client.root?"/+":this.Path+"/+");
        }
        if(chChanged) {
          this.RaisePropertyChanged("Children");
        }
        return _children;
      }
    }

    public string Path { get { return _parent==null?"/":(_parent==_client.root?"/"+Name:(_parent as TopicM).Path+"/"+Name); } }
    public TopicM Parent { get { return _parent as TopicM; } }
    public string ContentId { get { return GetUri("view="+View.ToString()); } }
    public Projection View { get; set; }
    public IEnumerable<TopicM> NameList { get { return _parent==null?(new TopicM[] { this }):(_parent as TopicM).NameList.Union(new TopicM[] { this }); } }

    public bool IsRoot { get; private set; }
    public int sizeX { get; set; }
    public int sizeY { get; set; }
    public double posX { get; set; }
    public double posY { get; set; }

    public TopicM Get(string p, bool create=true, bool wait=true) {
      TopicM cur;
      TopicM next=null;
      bool chExist;
      if(!string.IsNullOrEmpty(p) && p.StartsWith("/")) {
        cur=_client.root;
      } else {
        cur=this;
      }
      if(string.IsNullOrEmpty(p)) {
        return cur;
      }
      string[] pe=p.Split(_delmiter, StringSplitOptions.RemoveEmptyEntries);
      for(int i=0; i<pe.Length; i++, cur=next) {
        if((wait || !create) && (cur._subscribed&2)==0) {
          var t=_client.Subscribe(cur==_client.root?"/+":cur.Path+"/+");
          if(t!=null && wait) {
            t.Wait();
          }
        }

        if(cur._children==null) {
          next=null;
          chExist=false;
        } else {
          next=cur._children.FirstOrDefault(z => z.Name==pe[i]);
          chExist=next!=null;
        }

        if(!chExist) {
          if(create) {
            if(pe[i]=="+" || pe[i]=="#") {
              throw new ArgumentException("path ("+Path+") is not valid");
            } 
            if(cur._children==null) {
              cur._children=new ObservableTopics();
            }
            next=cur._children.AddTopic(cur, pe[i]);
          } else {
            return null;
          }
        }
      }
      return cur;
    }
    //public async Task<TopicM> GetAsync(string p, bool create=true) {
    //}
    public void AddChild() {
      Children.Insert(0, new TopicM(this, string.Empty));
    }
    public void Move(string npath, string nname) {
      TopicM np=this.Parent;
      if(string.IsNullOrEmpty(npath) || npath==np.Path) {  // rename
        int j=-1-np._children.IndexOf(this.Name);
        if(j<0) {
          X13.lib.Log.Warning("Move({0}, {1}, {2}) - source not found", this.Path, npath, nname);
          return;
        }
        int i=np._children.IndexOf(nname);
        if(j==(i>j?i-1:i)) {  // position is not changed
          // do nothing
        } else if(i>=0) {
          np._children.Move(j, i>j?i-1:i);
        } else {    // name already exist
          i=-1-i;
          np._children[i].Remove(false);
          np._children.Move(i<j?j-1:j, i>j?i-1:i);
        }
      } else {
        this.Parent._children.Remove(this);
        np=_client.root.Get(npath, true);
        this._parent=np;
        int i;
        if(np._children==null){
          i=0;
          np._children=new ObservableTopics();
        } else {
          i=np._children.IndexOf(nname);
        }
        if(i>=0) {
          np._children.Insert(i, this);
        } else {   // name already exist
          np._children[-1-i]=this;
        }
      }
      this.Name=nname;
      this.RaisePropertyChanged("Name");
      this.RaisePropertyChanged("Path");
      this.RaisePropertyChanged("ContentId");
      this.RaisePropertyChanged("NameList");

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
          _client.Publish(this.Path, "null");
          this.Remove(false);
        }
      } else {  // rename
        if(nname==Name) {  // rename, chancel

        } else {          // rename
          _client.Move(this.Path, (_parent as TopicM).Path, nname);
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
      _client.Publish(this.Path, json);
    }
    public override void Remove(bool ext) {
      if(this!=_client.root) {
        if(ext) {
          _client.Publish(this.Path, string.Empty);
        } else {
          if(_children!=null) {
            for(int i=_children.Count-1; i>=0; i--) {
              _children[i].Remove(ext);
            }
          }
          Parent._children.Remove(this);
          Workspace.This.CloseFile(this);
        }
      }
    }
    public override string GetUri(string p) {
      StringBuilder sb=new StringBuilder();
      if(IsRoot) {
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
      _client.Unsubscribe(this.Path, _subscribed); // path/+
    }

    internal class ObservableTopics : ObservableCollection<TopicM> {
      // Override the event so this class can access it
      public override event NotifyCollectionChangedEventHandler CollectionChanged;
      public TopicM AddTopic(TopicM parent, string name) {
        TopicM next;
        using(BlockReentrancy()) {
          int idx=IndexOf(name);
          if(idx<0) {
            next=this[-1-idx];
          } else {
            next=new TopicM(parent, name);
            this.Insert(idx, next);
          }
        }
        return next;
      }
      public int IndexOf(string name) {
        int i, j;
        for(i=this.Count-1;i>=0;i--) {
          if(this[i].EditName) {
            continue;
          }
          j=string.Compare(this[i].Name, name);
          if(j==0) {
            return -1-i;
          }
          if(j<0) {
            return i+1;
          }
        }
        return 0;
      }
      protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
        // Be nice - use BlockReentrancy like MSDN said
        using(BlockReentrancy()) {
          var eventHandler = CollectionChanged;
          if(eventHandler != null) {
            Delegate[] delegates = eventHandler.GetInvocationList();
            // Walk thru invocation list
            foreach(NotifyCollectionChangedEventHandler handler in delegates) {
              var dispatcherObject = handler.Target as DispatcherObject;
              // If the subscriber is a DispatcherObject and different thread
              if(dispatcherObject != null && dispatcherObject.CheckAccess() == false)
                // Invoke handler in the target dispatcher's thread
                dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, e);
              else // Execute handler as is
                handler(this, e);
            }
          }
        }
      }
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
