using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using X13.lib;
using X13.PLC;

namespace X13.Server {
  internal class Server {
    private HttpServer _sv;
    private Topic verbose;

    public Server() {

    }
    public void Start() {
      verbose=Topic.root.Get("/etc/Server/verbose");
      _sv = new HttpServer(8080);
      _sv.Log.Output=WsLog;
#if DEBUG
      _sv.Log.Level=WebSocketSharp.LogLevel.Trace;
#endif
      _sv.AddWebSocketService<ApiV04>("/api/v04");
      _sv.Start();
      if(_sv.IsListening) {
        Log.Info("Server started on {0}:{1} ", Environment.MachineName, _sv.Port.ToString());
      } else {
        Log.Error("Server start failed");
      }
    }
    public void Stop() {
      if(_sv!=null) {
        _sv.Stop();
      }
      _sv=null;
    }
    private void WsLog(LogData d, string f) {
      if(verbose.As<bool>()) {
        Log.Debug("WS({0}) - {1}", d.Level, d.Message);
      }
    }
  }
}
