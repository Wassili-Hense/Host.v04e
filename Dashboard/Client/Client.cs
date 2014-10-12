using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace X13.Client {
  internal class Client {
    private System.ComponentModel.BackgroundWorker _bgw;
    private ConcurrentQueue<MessageV04> _toSend;
    private ConcurrentQueue<MessageV04> _recived;

    public Client() {
    }
    public void Start() {
      _bgw=new System.ComponentModel.BackgroundWorker();
      _bgw.WorkerReportsProgress=true;
      _bgw.ProgressChanged+=ClientCB;
    }
    public void Stop() {
    }

    private void ClientCB(object sender, System.ComponentModel.ProgressChangedEventArgs e) {
      throw new NotImplementedException();
    }

  }
}
