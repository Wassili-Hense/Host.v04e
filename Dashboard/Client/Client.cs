using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using WebSocketSharp;
using X13.lib;
using System.Threading;

namespace X13.Client {
  internal class Client {
    private WebSocket _ws;
    private int _msgIdG;


    public Client(string url) {
      this.url=url;
      _msgIdG=1+((new Random()).Next(1, 0x7FFFFD) & 0x7FFFFE);
    }
    public void Start() {
      
      _ws=new WebSocket("ws://"+this.url+"/api/v04");
#if DEBUG
      _ws.Log.Output=WsLog;
#endif
      _ws.OnOpen+=_ws_OnOpen;
      _ws.OnMessage+=_ws_OnMessage;
      _ws.OnError+=_ws_OnError;
      _ws.OnClose+=_ws_OnClose;
      _ws.ConnectAsync();
    }

    public void Stop() {
    }
    public string url { get; private set; }
    public event Action<MessageV04> recv;

    private void _ws_OnOpen(object sender, EventArgs e) {
      Log.Info("client connected");
    }
    private void _ws_OnMessage(object sender, MessageEventArgs e) {
      MessageV04 msg;
      if(e.Type==Opcode.Text) {
        msg=MessageV04.Parse(e.Data);
      } else {
        return;
      }
      switch(msg.cmd) {
      case MessageV04.Cmd.Info:
        SendIntern(new MessageV04(MessageV04.Cmd.Connect, GenMsgId(), "user", "pass"));
        break;
      }
    }
    private void _ws_OnError(object sender, WebSocketSharp.ErrorEventArgs e) {
      Log.Error("client - ", e.Message);
    }
    private void _ws_OnClose(object sender, CloseEventArgs e) {
      Log.Info("client.closed {0}", e.Code);
    }
    private int GenMsgId() {
      int id, old;
      do{
        old=_msgIdG;
        id=old>0xFFFFFC?1:old+2;
      } while(Interlocked.CompareExchange(ref _msgIdG, id, old)!=old);
      return id;
    }
    private void SendIntern(MessageV04 msg) {
      if(_ws!=null && _ws.ReadyState==WebSocketState.Open) {
        _ws.Send(msg.ToString());
      }
    }
    private void WsLog(LogData d, string f) {
      Log.Debug("Client({0}) - {1}", d.Level, d.Message);
    }
  }
}
