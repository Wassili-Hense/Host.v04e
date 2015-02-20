using System;
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

    private WsClient() {
      _conn=WsServer.instance.Connect(RcvEvent);
    }
    public string Info { get { return "local"; } }

    public void Subscribe(string path, int mask) {
      _conn.Subscribe(path, mask);
    }
    public void Unsubscribe(string path, int mask) {
      _conn.Unsubscribe(path, mask);
    }

    internal void Create(string path, string payload) {
      _conn.Create(path, payload, string.Empty);
    }
    public void Publish(string path, string payload, string options=null) {
      _conn.Publish(path, payload, options);
    }
    public event Action<string, string, string> Event;

    public void Dispose() {
      var c=_conn;
      _conn=null;
      if(c!=null) {
        WsServer.instance.Disconnect(c);
      }
    }

    private void RcvEvent(string path, string payload, string options=null) {
      if(Event!=null) {
        Event(path, payload, options);
      }
    }

  }
}
