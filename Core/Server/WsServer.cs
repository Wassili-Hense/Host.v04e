using JST = NiL.JS.BaseLibrary;
using NiL.JS.Core;
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
      //Topic.root.Get("/A/DateTime\nNow").Set(DateTime.Now);
      Topic.root.Get("/Hello").Set("World");
      Topic.root.Get("/A/Test/T0").Set(0);

      Topic.root.Get("/etc/PLC/func/INC").SetJson("{\"$type\":\"PiDeclarer\",\"calc\":\"this.Q=this.A+1;\",\"pins\":{\"A\":{\"pos\":\"A\",\"mandatory\":true},\"Q\":{\"pos\":\"a\",\"mandatory\":true}}}");
      Topic.root.Get("/etc/PLC/func/DEC").SetJson("{\"$type\":\"PiDeclarer\",\"calc\":\"this.Q=this.A-1;\",\"pins\":{\"A\":{\"pos\":\"A\",\"mandatory\":true},\"Q\":{\"pos\":\"a\",\"mandatory\":true}}}");
      Topic.root.Get("/etc/PLC/func/MOD").SetJson("{\"$type\":\"PiDeclarer\",\"calc\":\"this.Q=this.A%this.B;\",\"pins\":{\"A\":{\"pos\":\"A\",\"mandatory\":true},\"B\":{\"pos\":\"B\",\"mandatory\":true},\"Q\":{\"pos\":\"a\",\"mandatory\":true}}}");

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
    private Dictionary<SubRec, long> _subscriptions;
    private Topic _owner;

    internal WsConnection(Action<string> re) {
      _rcvEvent=re;
      _owner=Topic.root.Get("/clients").Get(string.Format("{0}_{1}", Environment.MachineName, (Environment.TickCount&0xFFFF).ToString("X4")));
      _subscriptions=new Dictionary<SubRec, long>();
    }

    private void ChangedEvent(SubRec sb, Perform p) {
      string msg;
      if((p.art==Perform.Art.changed && p.prim!=_owner) || p.art==Perform.Art.subscribe || (p.art==Perform.Art.create && p.prim==_owner)) {
        msg="[36,"+ JsEnc(p.src.path) + ","+ p.src.ToJson() +"]";
        X13.lib.Log.Debug("S {0} Publish({1}, {2})", _owner==null?"Unk":_owner.name, p.src.path, p.src.ToJson());
      } else if(p.art==Perform.Art.remove) {
        msg="[36,"+ JsEnc(p.src.path) +"]";
        X13.lib.Log.Debug("S {0} Publish({1}) - Delete", _owner==null?"Unk":_owner.name, p.src.path);
      } else if(p.art==Perform.Art.move) {
        Topic nt=p.o as Topic;
        if(nt!=null) {
          msg="[296,"+JsEnc(p.src.path) + "," + JsEnc(nt.parent.path) + "," + JsEnc(nt.name) +"]";
          X13.lib.Log.Debug("S {0} Move({1}, {2})", _owner==null?"Unk":_owner.name, p.src.path, nt.path);
        } else {
          return;
        }
      } else if(p.art==Perform.Art.subAck){
        long sid;
        lock(_subscriptions){
          if(!_subscriptions.TryGetValue(sb, out sid)){
            sid=0;
          }
        }
        if(sid>0) {
          msg="[33,"+ sid.ToString()+"]";
          X13.lib.Log.Debug("S {0} SubAck({1})", _owner==null?"Unk":_owner.name, sb);
        } else {
          return;
        }
      } else if(p.art==Perform.Art.unsubAck){
        long sid;
        lock(_subscriptions) {
          if(!_subscriptions.TryGetValue(sb, out sid)) {
            sid=0;
          } else {
            _subscriptions.Remove(sb);
          }
        }
        if(sid>0) {
          msg="[35,"+ sid.ToString()+"]";
          X13.lib.Log.Debug("S {0} UnsubAck({1})", _owner==null?"Unk":_owner.name, sb);
        } else {
          return;
        }
      } else {
        return;
      }
      _rcvEvent(msg);
    }
    public void RcvMsg(string json) {
      string info=null;
      try {
        var msg=JST.JSON.parse(json) as JST.Array;
        if(msg!=null && msg.length.As<int>()>0 && msg["0"].IsNumber) {
          int cmd=msg["0"].As<int>();
          int len=msg.length.As<int>();
          switch(cmd) {
          case 16:
            if(len==3) { // Publish  [16, "path", payload]
              var tmp=_owner.Get(msg["1"].As<string>(), true, _owner);
              tmp.SetJson(msg["2"], _owner);
              info="publish";
            } else if(len==2) { // Remove  [16, "path"]
              var tmp=_owner.Get(msg["1"].As<string>(), false, _owner);
              if(tmp!=null) {
                tmp.Remove(_owner);
                info="remove";
              }
            }
            break;
          case 274:    // Copy  [274, "oldPath", "newParentPath", "neName"]
            if(len==4) {
              Copy(msg["1"].As<string>(), msg["2"].As<string>(), msg["3"].As<string>());
              info="copy";
            }
            break;
          case 276:  // Move   [276, "oldPath", "newParentPath", "neName"]
            if(len==4) {
              Topic o, p;
              if((o=_owner.Get(msg["1"].As<string>(), false, _owner))!=null && (p=_owner.Get(msg["2"].As<string>(), false, _owner))!=null) {
                o.Move(p, msg["3"].As<string>(), _owner);
                info="move";
              }
            }
            break;
          case 32:  //Subscribe   [32, sid, "mask"]
            if(len==3 && msg["1"].IsNumber) {
              SubRec sb;
              SubRec.SubMask msk=SubRec.SubMask.Once;
              long sId=msg[1].As<long>();
              string path=msg["2"].As<string>();
              if(path.EndsWith("/+")) {
                path=path.Substring(0, path.Length-1);
                msk=SubRec.SubMask.Chldren;
              } else if(path.EndsWith("/#")) {
                path=path.Substring(0, path.Length-1);
                msk=SubRec.SubMask.All;
              }
              var tmp=_owner.Get(path, true, _owner);
              sb=tmp.Subscribe(ChangedEvent, msk, false);
              lock(_subscriptions) {
                _subscriptions[sb]=sId;
              }
              info="subscribe";
            }
            break;
          case 34:  //Unubscribe   [34, sid]
            if(len==2 && msg["1"].IsNumber) {
              long sId=msg[1].As<long>();
              SubRec sb;
              lock(_subscriptions) {
                var kv=_subscriptions.FirstOrDefault(z => z.Value==sId);
                sb=kv.Key;
              }
              if(sb!=null) {
                var tmp=_owner.Get(sb.path, false, _owner);
                if(tmp!=null) {
                  tmp.Unsubscribe(ChangedEvent, sb.flags, false);
                }
              }
              info="unsubscribe";
            }
            break;
          }
        }
        X13.lib.Log.Debug("R {0} {1}", _owner==null?"Unk":_owner.name, info==null?json:("["+info+json.Substring(json.IndexOf(','))));
      }
      catch(Exception ex) {
        X13.lib.Log.Debug("R {0} {1} - {2}", _owner==null?"Unk":_owner.name, json, ex.Message);
      }
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
    public override string ToString() {
      return _owner==null?base.ToString():_owner.name;
    }
  }
}
