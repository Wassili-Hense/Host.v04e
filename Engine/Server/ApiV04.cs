using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using X13.PLC;

namespace X13.Server {
  internal class ApiV04 : WebSocketBehavior {
    private static Topic _verbose;
    private static Timer _pingTimer;
    private static WebSocketSessionManager _wsMan;
    private static string _hostName;

    static ApiV04() {
      _verbose=Topic.root.Get("/etc/Server/verbose");
      _pingTimer=new Timer(PingF, null, 270000, 300000);
      _hostName=Environment.MachineName;
    }
    private static void PingF(object o) {
      if(_wsMan!=null) {
        _wsMan.Broadping();
      }
    }

    private Session _ses;

    protected override void OnOpen() {
      if(_wsMan==null) {
        _wsMan=Sessions;
      }
      string sid=null;
      if(Context.CookieCollection["sessionId"]!=null) {
        sid=Context.CookieCollection["sessionId"].Value;
      }
      System.Net.IPEndPoint remoteEndPoint = Context.UserEndPoint;
      {
        System.Net.IPAddress remIP;
        if(Context.Headers.Contains("X-Real-IP") && System.Net.IPAddress.TryParse(Context.Headers["X-Real-IP"], out remIP)) {
          remoteEndPoint=new System.Net.IPEndPoint(remIP, remoteEndPoint.Port);
        }
      }
      _ses=Session.Get(sid, remoteEndPoint, false);
      if(_ses!=null) {
        Send((new MessageV04(MessageV04.Cmd.Ack, 0, _ses.id)).ToString());
      } else {
        Send((new MessageV04(MessageV04.Cmd.Info, 0, _hostName)).ToString());
      }
      Send(string.Concat("I\t", _ses.id, "\t", string.IsNullOrEmpty(_ses.userName)?"null":"true") );
      if(_verbose.As<bool>()) {
        X13.lib.Log.Debug("{0} connect webSocket", _ses.owner.name);
      }
    }
    protected override void OnMessage(MessageEventArgs e) {
    }
    protected override void OnError(ErrorEventArgs e) {
    }
    protected override void OnClose(CloseEventArgs e) {
    }
  }
}
