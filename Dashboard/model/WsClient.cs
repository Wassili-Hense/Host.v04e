using NiL.JS.Core;
using NiL.JS.Core.Modules;
using JST = NiL.JS.Core.BaseTypes;
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

    public void Subscribe(string path, int mask) {
      _conn.Subscribe(path, mask);
    }
    public void Unsubscribe(string path, int mask) {
      _conn.Unsubscribe(path, mask);
    }

    public void Create(string path, string payload) {
      _conn.Create(path, payload, string.Empty);
    }
    public void Publish(string path, string payload, string options=null) {
      _conn.Publish(path, payload, options);
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
        var jo=JSON.parse(json) as JST.Array;
        if(jo!=null && jo.length.As<int>()>0 && jo["0"].IsNumber) {
          _ipq.Enqueue(jo);
        }
        X13.lib.Log.Debug("Rcv: {0}", json);
      }
      catch(Exception ex) {
        X13.lib.Log.Debug("RcvMsg({0}) - {1}", json, ex.Message);
      }
    }

    internal void Move(string path, string parentPath, string nname) {
      _conn.Move(path, parentPath, nname);
    }
    internal void Copy(string path, string parentPath, string nname) {
      _conn.RcvMsg("[18,"+JsEnc(path) + "," + JsEnc(parentPath) + "," + JsEnc(nname) +"]");
    }
  }
}
