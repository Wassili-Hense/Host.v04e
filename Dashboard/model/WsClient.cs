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

    private WsClient() {
      _ipq=new ConcurrentQueue<JST.Array>();
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

    public void Subscribe(string path, int mask, bool waitAck=false) {
      StringBuilder sb=new StringBuilder();
      sb.Append("[32,");
      sb.Append(JsEnc(path));
      sb.Append(",{");
      if((mask&1)==1) {
        sb.Append("\"once\":true");
      }
      if((mask&2)==2) {
        if((mask&1)!=0) {
          sb.Append(",");
        }
        sb.Append("\"children\":true");
      }
      if((mask&4)==4) {
        if((mask&3)!=0) {
          sb.Append(",");
        }
        sb.Append("\"all\":true");
      }
      sb.Append("}]");
      _conn.RcvMsg(sb.ToString());
    }
    public void Unsubscribe(string path, int mask) {
      StringBuilder sb=new StringBuilder();
      sb.Append("[34,");
      sb.Append(JsEnc(path));
      sb.Append(",{");
      if((mask&1)==1) {
        sb.Append("\"once\":true");
      }
      if((mask&2)==2) {
        if((mask&1)!=0) {
          sb.Append(",");
        }
        sb.Append("\"children\":true");
      }
      if((mask&4)==4) {
        if((mask&3)!=0) {
          sb.Append(",");
        }
        sb.Append("\"all\":true");
      }
      sb.Append("}]");
      _conn.RcvMsg(sb.ToString());
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
