﻿using System;
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
      Topic.root.Get("/A/v1").Set(true);
      Topic.root.Get("/A/i1").Set(157);
      Topic.root.Get("/A/d1").Set(9.81);
      Topic.root.Get("/A/dt1").Set(DateTime.Now);
      Topic.root.Get("/Hello").Set("World");
      _plcTick=new System.Threading.Timer(PlcTick, null, 50, 100);
    }
    public WsConnection Connect(Action<string, string, string> re) {
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
    private Action<string, string, string> _rcvEvent;
    private Topic _owner;

    internal WsConnection(Action<string, string, string> re) {
      _rcvEvent=re;
      _owner=Topic.root.Get("/clients").Get(string.Format("{0}_{1}", Environment.MachineName, (Environment.TickCount&0xFFFF).ToString("X4")));
    }
    public void Subscribe(string path, int mask) {
      var tmp=_owner.Get(path, true, _owner);
      if((mask&1)!=0) {
        tmp.changed+=ChangedEvent;
      }
      if((mask&2)!=0) {
        tmp.children.changed+=ChangedEvent;
      } else if((mask&4)!=0) {
        tmp.all.changed+=ChangedEvent;
      }
    }

    void ChangedEvent(Topic s, Perform p) {
      if(p.prim==_owner){
        return;
      }
      if(p.art==Perform.Art.changed  || p.art==Perform.Art.subscribe){
        _rcvEvent(s.path, s.ToJson(), null);
      }
    }

    public void Unsubscribe(string path, int mask) {
      var tmp=_owner.Get(path, false, _owner);
      if(tmp==null) {
        return;
      }
      if((mask&1)!=0) {
        tmp.changed-=ChangedEvent;
      }
      if((mask&2)!=0) {
        tmp.children.changed-=ChangedEvent;
      } else if((mask&4)!=0) {
        tmp.all.changed-=ChangedEvent;
      }
    }
    public void Publish(string path, string payload, string options) {
      var tmp=_owner.Get(path, true, _owner);
      tmp.SetJson(payload, _owner);      
    }
  }
}
