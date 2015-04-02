using NiL.JS.Core;
using JST = NiL.JS.BaseLibrary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using X13.Server;
using System.Threading.Tasks;
using System.Threading;
using MIm=System.Windows.Media.Imaging;

namespace X13.model {
  internal class WsClient : IDisposable {
    private static string JsEnc(string s) {
      return System.Web.HttpUtility.JavaScriptStringEncode(s, true);
    }
    private static WsClient _local;

    static WsClient() {
      _local=new WsClient();
    }
    public static WsClient Get(string p) {
      return _local;
    }

    public readonly TopicM root;

    private WsConnection _conn;
    private long _subIdx;
    private Dictionary<long, string> _subscriptions;
    private ConcurrentDictionary<long, TaskCompletionSource<long>> _waitAcks;
    private SortedList<string, DeclarerM> _declarers;

    private WsClient() {
      root=TopicM.CreateRoot(this);
      _subIdx=1;
      _subscriptions=new Dictionary<long, string>();
      _waitAcks=new ConcurrentDictionary<long, TaskCompletionSource<long>>();

      _declarers=new SortedList<string, DeclarerM>();
      _declarers[ViewTypeEn.Bool]     =new DeclarerM(ViewTypeEn.Bool) { View=ViewTypeEn.Bool, Icon=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_bool.png", UriKind.Relative)) };
      _declarers[ViewTypeEn.Int]      =new DeclarerM(ViewTypeEn.Int) { View=ViewTypeEn.Int, Icon=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_i64.png", UriKind.Relative)) };
      _declarers[ViewTypeEn.Double]   =new DeclarerM(ViewTypeEn.Double) { View=ViewTypeEn.Double, Icon=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_f02.png", UriKind.Relative)) };
      _declarers[ViewTypeEn.DateTime] =new DeclarerM(ViewTypeEn.DateTime) { View=ViewTypeEn.DateTime, Icon=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_dt.png", UriKind.Relative)) };
      _declarers[ViewTypeEn.String]   =new DeclarerM(ViewTypeEn.String) { View=ViewTypeEn.String, Icon=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_str.png", UriKind.Relative)) };
      _declarers[ViewTypeEn.PiAlias]  =new DeclarerM(ViewTypeEn.PiAlias) { View=ViewTypeEn.Object, Icon=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_ref.png", UriKind.Relative)) };
      _declarers[ViewTypeEn.PiLink]   =new DeclarerM(ViewTypeEn.PiLink) { View=ViewTypeEn.Object, Icon=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_wire.png", UriKind.Relative)) };
      _declarers[ViewTypeEn.Object]   =new DeclarerM(ViewTypeEn.Object) { View=ViewTypeEn.Object, Icon=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_obj.png", UriKind.Relative)) };
      //_declarers[]=new DeclarerM() { Name=, View=, Icon=

      _conn=WsServer.instance.Connect(RcvMsg);
    }
    public string Info { get { return "local"; } }

    public void Publish(string path, string payload, string options=null) {
      if(string.IsNullOrEmpty(payload)) {
        _conn.RcvMsg("[16,"+JsEnc(path) + "]");
      } else {
        _conn.RcvMsg("[16,"+JsEnc(path) + "," + payload +"]");
      }
    }
    public void Copy(string path, string parentPath, string nname) {
      _conn.RcvMsg("[274,"+JsEnc(path) + "," + JsEnc(parentPath) + "," + JsEnc(nname) +"]");
    }
    public void Move(string path, string parentPath, string nname) {
      _conn.RcvMsg("[276,"+JsEnc(path) + "," + JsEnc(parentPath) + "," + JsEnc(nname) +"]");
    }

    public Task<long> Subscribe(string mask) {
      long sid;
      bool exist=false;

      lock(_subscriptions) {
        var kv=_subscriptions.FirstOrDefault(z => z.Value==mask);
        if(kv.Key>0) {
          sid=kv.Key;
          exist=true;
        } else {
          sid=_subIdx++;
          _subscriptions[sid]=mask;
        }
      }
      TaskCompletionSource<long> tsk;
      if(!exist) {
        tsk=new TaskCompletionSource<long>();
        _waitAcks[sid]=tsk;
        _conn.RcvMsg("[32,"+sid.ToString()+","+JsEnc(mask)+"]");
      } else if(!_waitAcks.TryGetValue(sid, out tsk)) {
        return null;
      }
      return tsk.Task;
    }
    public void Unsubscribe(string path, int mask) {
      if((mask&6)!=0) {
        if(path=="/") {
          path=string.Empty;
        }
        if((mask&2)==2) {
          path+="/+";
        } else {
          path+="/#";
        }
      }
      long sid=0;
      lock(_subscriptions) {
        var kv=_subscriptions.FirstOrDefault(z => z.Value==path);
        if(kv.Value!=null) {
          sid=kv.Key;
        }
      }
      if(sid>0) {
        _conn.RcvMsg("[34,"+sid.ToString()+"]");
      }

    }

    public DeclarerM GetDecl(string name) {
      DeclarerM d;
      if(!_declarers.TryGetValue(name, out d)) {
        d=_declarers[ViewTypeEn.Object];
      }
      return d;
    }


    public void Dispose() {
      var c=_conn;
      _conn=null;
      if(c!=null) {
        WsServer.instance.Disconnect(c);
      }
    }

    private void RcvMsg(string json) {
      try {
        var jo=JST.JSON.parse(json) as JST.Array;
        if(jo!=null && jo.length.As<int>()>0 && jo["0"].IsNumber) {
          ProcMsgs(jo);
        }
      }
      catch(Exception ex) {
        X13.lib.Log.Warning("RcvMsg({0}) - {1}", json, ex.Message);
      }
    }
    private void ProcMsgs(JST.Array msg) {
      int len;
      int cmd=msg["0"].As<int>();
      len=msg.length.As<int>();
      switch(cmd) {
      case 36: {
          string path=msg["1"].As<string>();
          if(string.IsNullOrEmpty(path)) {
            break;
          }
          if(len>2) {     // [Event, "path", value, options]
            var t=root.Get(path, true, false);
            t.SetValue(msg["2"]);
          } else if(len==2) {  //[Event, "path"] - remove
            var t=root.Get(path, false, false);
            if(t!=null) {
              t.Remove(false);
            }
          }
        }
        break;
      case 296:
        if(len==4) {
          string opath=msg["1"].As<string>();
          string npath=msg["2"].As<string>();
          string nname=msg["3"].As<string>();
          TopicM ot;
          if(!string.IsNullOrEmpty(opath) && !string.IsNullOrEmpty(nname) && (ot=root.Get(opath, false, false))!=null) {
            ot.Move(npath, nname);
          }
        }
        break;
      case 33:    // SubAck
        if(len==2 && msg[1].IsNumber) {
          long sid=msg[1].As<long>();
          string mask;
          lock(_subscriptions) {
            if(!_subscriptions.TryGetValue(sid, out mask)) {
              mask=null;
            }
          }
          if(mask!=null) {
            if(mask.EndsWith("/+")) {
              var t=root.Get(mask.Substring(0, mask.Length-1), false, false);
              if(t!=null) {
                t._subscribed|=2;
              }
            } else if(mask.EndsWith("/#")) {
              var t=root.Get(mask.Substring(0, mask.Length-1), false, false);
              if(t!=null) {
                t._subscribed|=4;
              }
            } else {
              var t=root.Get(mask, false, false);
              if(t!=null) {
                t._subscribed|=1;
              }
            }
          }
          TaskCompletionSource<long> tsk;
          if(_waitAcks.TryGetValue(sid, out tsk)) {
            tsk.SetResult(sid);
          }
        }
        break;
      case 35:    // UnsubAck
        if(len==2 && msg[1].IsNumber) {
          long sid=msg[1].As<long>();
          lock(_subscriptions) {
            _subscriptions.Remove(sid);
          }
        }
        break;
      }
    }
  }
}
