using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace X13 {
  class Program {
    private static Server.Server _srv;
    private static PLC.PLC _plc;
    static void Main(string[] args) {
      _plc=PLC.PLC.instance;
      _srv=new Server.Server();
      _plc.Init();
      _plc.Start();
      _plc.Tick();
      _plc.Tick();
      _srv.Start();
      while(!Console.KeyAvailable) {
        Thread.Sleep(100);
        _plc.Tick();
      }
      _srv.Stop();
      _plc.Stop();
    }
  }
}
