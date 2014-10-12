using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using X13.lib;

namespace X13.Client {
  internal struct MessageV04 {
    private const string AB="0123456789AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz$#";
    private static MessageV04 Null;

    static MessageV04() {
      Null=new MessageV04(Cmd.Empty, 0, null);
    }

    public static MessageV04 Parse(string msg) {
      if(msg==null || msg.Length<2) {
        return Null;
      }
      MessageV04 m=Null;
      Cmd cmd=(Cmd)msg[0];
      long mid=0;
      int pos=1;
      if(msg[pos]!=' ') {
        if(msg.Length<5) {
#if DEBUG
          Log.Warning("MessageV04({0}) - MId is too short", msg);
#endif
          return Null;
        }
        int j;
        for(; pos<5; pos++) {
          j=AB.IndexOf(msg[pos]);
          if(j<0) {
#if DEBUG
            Log.Warning("MessageV04({0}) - bad char in MId", msg);
#endif
            return Null;
          }
          mid=(mid*64)+j;
        }
        string[] payload;
        if(msg.Length>pos) {
          payload=msg.Substring(pos).Split('\x1E');
        }
      }

      return m;
    }

    public readonly Cmd cmd;
    public readonly long mid;
    public readonly string[] payload;
    public MessageV04(Cmd cmd, long mid, params string[] payload) {
      this.cmd=cmd;
      this.mid=mid;
      this.payload=payload;
    }
    public override string ToString() {
      StringBuilder sb=new StringBuilder();
      sb.Append((char)this.cmd);
      if(mid==0) {
        sb.Append(' ');
      } else {
        sb.Append(AB[(int)((mid>>18)&63)]);
        sb.Append(AB[(int)((mid>>12)&63)]);
        sb.Append(AB[(int)((mid>>6)&63)]);
        sb.Append(AB[(int)(mid&63)]);
      }
      if(payload!=null) {
        for(int i=0; i<payload.Length; i++) {
          if(i>0) {
            sb.Append('\x1E');
            if(!string.IsNullOrEmpty(payload[i])) {
              sb.Append(payload[i]);
            }
          }
        }
      }

      return sb.ToString();
    }
    public enum Cmd : ushort {
      Empty=' ',
      Info='I',       // ServerName
      Connect='C',    //  MId , UName, UPass
      Get='G',        // [MId], Mask
      SubTopic='S',   // [MId], Mask
      UnsubTopic='U', // [MId], Mask
      PubTopic='P',   // [MId], Path, Payload
      Dir='D',        // [MId], Path
      SubDir='s',     // [MID], Path
      UnsubDir='u',   // [MId], Path
      PubDir='p',     // [MId], Path, {entrys}
      Ack='A',        //  MId , [Info]
      Nack='N',       // [MId], Error
    }
  }
}
