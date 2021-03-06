﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using X13.lib;
using X13.PLC;

namespace X13.Server {
  internal class Session : IDisposable {
    private static List<WeakReference> sessions;
    private static Topic _verbose;

    static Session() {
      sessions=new List<WeakReference>();
      _verbose=Topic.root.Get("/etc/Server/verbose");
    }
    public static Session Get(string sid, System.Net.IPEndPoint ep, bool create=true) {
      Session s;
      if(string.IsNullOrEmpty(sid) || (s=sessions.Where(z => z.IsAlive).Select(z => z.Target as Session).FirstOrDefault(z => z!=null && z.id==sid && z.ip.Equals(ep.Address)))==null) {
        if(create) {
          s=new Session(ep);
          sessions.Add(new WeakReference(s));
        } else {
          s=null;
        }
      }
      return s;
    }

    private Session(System.Net.IPEndPoint ep) {
      _msgIdG=((new Random()).Next(2, 0x7FFFFD) & 0x7FFFFE);
      Topic r=Topic.root.Get("/etc/connections");
      this.id = Guid.NewGuid().ToString();
      this.ip = ep.Address;
      int i=1;
      string pre=ip.ToString();
      while(r.Exist(pre+i.ToString())) {
        i++;
      }
      _owner=r.Get(pre+i.ToString());
      owner.saved=false;
      try {
        var he=System.Net.Dns.GetHostEntry(this.ip);
        _host=string.Format("{0}[{1}]", he.HostName, this.ip.ToString());
        var tmp=he.HostName.Split('.');
        if(tmp!=null && tmp.Length>0 && !string.IsNullOrEmpty(tmp[0])) {
          i=1;
          while(r.Exist(tmp[0]+"-"+i.ToString())) {
            i++;
          }
          _owner.Move(r, tmp[0]+"-"+i.ToString());
        }
      }
      catch(Exception) {
        _host=string.Format("[{0}]", this.ip.ToString());
      }
      this.owner.Set(_host);
      Log.Info("{0} session[{2}] - {1}", owner.name, this._host, this.id);
    }
    private string _host;
    private Topic _owner;
    private int _msgIdG;

    public readonly string id;
    public readonly System.Net.IPAddress ip;
    public string userName;
    public Topic owner { get { return _owner; } }
    public void Close() {
      sessions.RemoveAll(z => !z.IsAlive || z.Target==this);
      Dispose();
    }
    public override string ToString() {
      return (string.IsNullOrEmpty(userName)?"anonymus":userName)+"@"+_host;
    }
    public int GenMsgId() {
      int id, old;
      do {
        old=_msgIdG;
        id=old>0xFFFFFD?2:old+2;
      } while(Interlocked.CompareExchange(ref _msgIdG, id, old)!=old);
      return id;
    }

    public void Dispose() {
      var o=Interlocked.Exchange(ref _owner, null);
      if(o!=null && !o.disposed) {
        o.Remove();
      }
    }
  }
}
