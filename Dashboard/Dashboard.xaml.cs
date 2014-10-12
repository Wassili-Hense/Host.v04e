using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace X13 {
  public partial class Dashboard: Window {
    private Client.Client _client;
    public Dashboard() {
      InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      _client=new Client.Client();
      _client.Start();
    }

    private void Window_Closing(object sender, CancelEventArgs e) {
      _client.Stop();
    }
  }
}
