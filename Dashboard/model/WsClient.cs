using NiL.JS.Core;
using JST = NiL.JS.BaseLibrary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using X13.Server;

namespace X13.model {
  internal class WsClient : IDisposable {
    private static string JsEnc(string s) {
      return System.Web.HttpUtility.JavaScriptStringEncode(s, true);
    }

    public static readonly WsClient instance;

    static WsClient() {
      instance = new WsClient();
    }

    private WsConnection _conn;
    private ConcurrentQueue<JST.Array> _ipq;
    private long _subIdx;
    private Dictionary<long, string> _subscriptions;

    private WsClient() {
      _ipq=new ConcurrentQueue<JST.Array>();
      _conn=WsServer.instance.Connect(RcvMsg);
      _subIdx=1;
      _subscriptions=new Dictionary<long, string>();
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

    public void Subscribe(string path, int mask, bool waitAck=false) {
      long sid;
      bool exist=false;
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
      lock(_subscriptions) {
        var kv=_subscriptions.FirstOrDefault(z => z.Value==path);
        if(kv.Key>0) {
          sid=kv.Key;
          exist=true;
        } else {
          sid=_subIdx++;
          _subscriptions[sid]=path;
        }
      }
      if(!exist) {
        _conn.RcvMsg("[32,"+sid.ToString()+","+JsEnc(path)+"]");
      }
      if(waitAck) {
        //TODO: WaitAck
      }
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
        var kv=_subscriptions.FirstOrDefault(z=>z.Value==path);
        if(kv.Value!=null) {
          sid=kv.Key;
        }
      }
      if(sid>0) {
        _conn.RcvMsg("[34,"+sid.ToString()+"]");
      }

    }

    public bool Poll(out JST.Array msg) {
      return _ipq.TryDequeue(out msg);
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
          _ipq.Enqueue(jo);
        }
      }
      catch(Exception ex) {
        X13.lib.Log.Warning("RcvMsg({0}) - {1}", json, ex.Message);
      }
    }
  }
}
