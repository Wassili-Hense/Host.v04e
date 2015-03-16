using NiL.JS.Core.Modules;
using JST = NiL.JS.Core.BaseTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using X13.PLC;

namespace X13.Server {
  public class WsServer {
    public static readonly WsServer instance;
    private static System.Threading.Timer _plcTick;

    static WsServer() {
      instance=new WsServer();
    }
    private WsServer() {
      PLC.PLC.instance.Init();
      PLC.PLC.instance.Tick();
      PLC.PLC.instance.Start();
      PLC.PLC.instance.Tick();
      Topic.root.Get("/A/W001").SetJson("{\"$type\":\"PiLink\",\"i\":\"v1\",\"o\":\"v2\"}");
      Topic.root.Get("/A/O1").SetJson("{\"A\":15,\"B\":{\"BA\":true},\"C\":19.62}");
      Topic.root.Get("/A/v1").Set(false);
      Topic.root.Get("/A/i1").Set(157);
      Topic.root.Get("/A/d1").Set(9.81);
      Topic.root.Get("/A/DateTime\nNow").Set(DateTime.Now);
      Topic.root.Get("/Hello").Set("World");
      Topic.root.Get("/A/Test/T0").Set(0);
      _plcTick=new System.Threading.Timer(PlcTick, null, 50, 100);
    }
    public WsConnection Connect(Action<string> re) {
      return new WsConnection(re);
    }
    public void Disconnect(WsConnection conn) {
      _plcTick.Change(-1, -1);
      System.Threading.Thread.Sleep(150);
      PLC.PLC.instance.Stop();
    }
    private void PlcTick(object o) {
      PLC.PLC.instance.Tick();
    }
  }
  public class WsConnection {
    private static string JsEnc(string s) {
      return System.Web.HttpUtility.JavaScriptStringEncode(s, true);
    }

    private Action<string> _rcvEvent;
    private Topic _owner;

    internal WsConnection(Action<string> re) {
      _rcvEvent=re;
      _owner=Topic.root.Get("/clients").Get(string.Format("{0}_{1}", Environment.MachineName, (Environment.TickCount&0xFFFF).ToString("X4")));
    }
    public void Subscribe(string path, int mask) {
      var tmp=_owner.Get(path, true, _owner);
      string m=string.Empty;
      if((mask&1)!=0) {
        tmp.changed+=ChangedEvent;
      }
      if((mask&2)!=0) {
        tmp.children.changed+=ChangedEvent;
        m="/+";
      } else if((mask&4)!=0) {
        tmp.all.changed+=ChangedEvent;
        m="/#";
      }
      X13.lib.Log.Debug("Subscribe({0}{1})", path, m);
    }

    private void ChangedEvent(Topic s, Perform p) {
      if((p.art==Perform.Art.changed && p.prim!=_owner) || p.art==Perform.Art.subscribe || (p.art==Perform.Art.create && p.prim==_owner)) {
        string json=s.ToJson();
        _rcvEvent("[36,"+ JsEnc(s.path) + ","+ json +"]");
      } else if(p.art==Perform.Art.remove) {
        _rcvEvent("[36,"+ JsEnc(s.path)+"]");
      } else if(p.art==Perform.Art.move) {
        Topic nt=p.o as Topic;
        if(nt!=null) {
          _rcvEvent("[40,"+JsEnc(s.path) + "," + JsEnc(nt.parent.path) + "," + JsEnc(nt.name) +"]");
        }
      }
    }
    public void RcvMsg(string json) {
      try {
        var msg=JSON.parse(json) as JST.Array;
        if(msg!=null && msg.length.As<int>()>0 && msg["0"].IsNumber) {
          int cmd=msg["0"].As<int>();
          int len=msg.length.As<int>();
          switch(cmd) {
          case 18:    // Copy
            if(len==4) {
              Copy(msg["1"].As<string>(), msg["2"].As<string>(), msg["3"].As<string>());
            }
            break;
          }
        }
        X13.lib.Log.Debug("{0} > {1}", _owner==null?"Unk":_owner.path, json);
      }
      catch(Exception ex) {
        X13.lib.Log.Debug("{0} > {1} - {2}", _owner==null?"Unk":_owner.path, json, ex.Message);
      }
    }


    public void Unsubscribe(string path, int mask) {
      var tmp=_owner.Get(path, false, _owner);
      string m=string.Empty;
      if(tmp==null) {
        return;
      }
      if((mask&1)!=0) {
        tmp.changed-=ChangedEvent;
      }
      if((mask&2)!=0) {
        tmp.children.changed-=ChangedEvent;
        m="/+";
      } else if((mask&4)!=0) {
        tmp.all.changed-=ChangedEvent;
        m="/#";
      }
      X13.lib.Log.Debug("Unsubscribe({0}{1})", path, m);
    }
    public void Publish(string path, string payload, string options) {
      if(string.IsNullOrEmpty(payload)) {
        var tmp=_owner.Get(path, false, _owner);
        if(tmp!=null) {
          tmp.Remove(_owner);
          X13.lib.Log.Debug("Publish({0}, , Remove)", path);
        }
      } else {
        var tmp=_owner.Get(path, true, _owner);
        tmp.SetJson(payload, _owner);
        X13.lib.Log.Debug("Publish({0}, {1})", path, payload);
      }
    }
    public void Create(string path, string payload, string options) {
      var tmp=_owner.Get(path, true, _owner);
    }
    public void Move(string path, string parentPath, string nname) {
      Topic o, p;
      if((o=_owner.Get(path, false, _owner))!=null && (p=_owner.Get(parentPath, false, _owner))!=null) {
        o.Move(p, nname, _owner);
      }
      X13.lib.Log.Debug("Move({0}, {1}, {2})", path, parentPath, nname);
    }
    private void Copy(string opath, string parentPath, string nname) {
      Topic ot, np, nt, n;
      if(string.IsNullOrEmpty(opath) || string.IsNullOrEmpty(nname) || string.IsNullOrEmpty(parentPath) || (ot=_owner.Get(opath, false, _owner))==null || parentPath==ot.parent.path) {
        return;
      }
      np=_owner.Get(parentPath, true, _owner);
      nt=np.Get(nname, true, _owner);
      int opLen=ot.path.Length+1;
      foreach(var t in ot.all) {
        if(t==ot) {
          n=nt;
        } else {
          n=nt.Get(t.path.Substring(opLen), true, _owner);
        }
        n.SetJson(t.ToJson(), _owner);
      }
    }
  }
}
