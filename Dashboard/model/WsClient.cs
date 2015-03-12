using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using X13.Server;

namespace X13.model {
  internal class WsClient : IDisposable {
    public static readonly WsClient instance;

    static WsClient() {
      instance = new WsClient();
    }

    private WsConnection _conn;
    private ConcurrentQueue<Tuple<string, string, string>> _ipq;

    private WsClient() {
      _ipq=new ConcurrentQueue<Tuple<string, string, string>>();
      _conn=WsServer.instance.Connect(RcvEvent);
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
    public bool Poll(out Tuple<string, string, string> msg) {
      return _ipq.TryDequeue(out msg);
    }
    public void Dispose() {
      var c=_conn;
      _conn=null;
      if(c!=null) {
        WsServer.instance.Disconnect(c);
      }
    }

    private void RcvEvent(string path, string payload, string options=null) {
      _ipq.Enqueue(new Tuple<string, string, string>(path, payload, options));
    }

  }
}
