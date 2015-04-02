using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using X13.lib;
using X13.model;


namespace X13.UI {
  /// <summary>
  /// Interaktionslogik für MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    private string _cfgPath;

    public MainWindow() {
      _cfgPath=Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+"/X13/Dashboard.cfg";
      InitializeComponent();
      this.DataContext = Workspace.This;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      bool wait=false;
      try {
        if(!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(_cfgPath))) {
          System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_cfgPath));
        } else if(System.IO.File.Exists(_cfgPath)) {
          var xd=new XmlDocument();
          xd.Load(_cfgPath);
          var window=xd.SelectSingleNode("/Config/Window");
          if(window!=null) {
            WindowState st;
            double tmp;
            if(window.Attributes["Top"]!=null && double.TryParse(window.Attributes["Top"].Value, out tmp)) {
              this.Top=tmp;
            }
            if(window.Attributes["Left"]!=null && double.TryParse(window.Attributes["Left"].Value, out tmp)) {
              this.Left=tmp;
            }
            if(window.Attributes["Width"]!=null && double.TryParse(window.Attributes["Width"].Value, out tmp)) {
              this.Width=tmp;
            }
            if(window.Attributes["Height"]!=null && double.TryParse(window.Attributes["Height"].Value, out tmp)) {
              this.Height=tmp;
            }
            if(window.Attributes["State"]!=null && Enum.TryParse(window.Attributes["State"].Value, out st)) {
              this.WindowState=st;
            }
          }
          var xlay=xd.SelectSingleNode("/Config/LayoutRoot");
          if(xlay!=null) {
            BackgroundWorker bw=new BackgroundWorker();
            var cl=WsClient.Get("local");  // ????????????
            bw.DoWork+=bw_DoWork;
            bw.ProgressChanged+=bw_ProgressChanged;
            bw.RunWorkerCompleted+=bw_RunWorkerCompleted;
            bw.WorkerReportsProgress=true;
            bw.RunWorkerAsync(xlay);
            wait=true;
          }
        }
      }
      catch(Exception ex) {
        Log.Error("Load config - {0}", ex.Message);
      }
      if(!wait) {
        if(!Workspace.This.Files.Any()) {
          Workspace.This.AddFile(WsClient.Get("local").root);
        }
        BusyIndicator.IsBusy=false;
      }
    }

    private void bw_DoWork(object sender, DoWorkEventArgs e) {
      var bg=sender as BackgroundWorker;
      var xlay=e.Argument as XmlNode;
      XmlAttribute cid_s;
      Uri ur;
      var urs=xlay.SelectNodes(".//LayoutDocument[@ContentId]");
      int i=0;
      foreach(XmlNode cid in urs) {
        if((cid_s=cid.Attributes["ContentId"])!=null && Uri.TryCreate(cid_s.Value, UriKind.Absolute, out ur) && ur.Scheme=="x13") {
          bg.ReportProgress((i++)*100/urs.Count, "Loading "+ur.Host+ur.AbsolutePath);
          var cl=WsClient.Get(ur.Host);
          var r=cl.root.Get(ur.AbsolutePath, false, true);
          var ch=r.Children;
        }
      }
      e.Result=xlay.OuterXml;
    }
    private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e) {
      BusyIndicator.BusyContent=e.UserState as string;
    }
    private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
      string layoutS=e.Result as string;
      if(layoutS!=null) {
        var layoutSerializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
        layoutSerializer.LayoutSerializationCallback+=layoutSerializer_LayoutSerializationCallback;
        layoutSerializer.Deserialize(new System.IO.StringReader(layoutS));
      }
      BusyIndicator.IsBusy=false;
    }
    private void layoutSerializer_LayoutSerializationCallback(object s, Xceed.Wpf.AvalonDock.Layout.Serialization.LayoutSerializationCallbackEventArgs e1) {
      if(!string.IsNullOrWhiteSpace(e1.Model.ContentId)) {
        var t=Workspace.This.Open(e1.Model.ContentId);
        if(t!=null) {
          e1.Content = t;
        } else {
          e1.Cancel=true;
        }
      }
    }

    private void dockManager_DocumentClosed(object sender, Xceed.Wpf.AvalonDock.DocumentClosedEventArgs e) {
      Workspace.This.CloseFile(e.Document.Content as TopicM);
    }
    private void BlocksPanel_MLD(object sender, MouseButtonEventArgs e) {

    }
    private void BlocksPanel_MLU(object sender, MouseButtonEventArgs e) {

    }
    private void BlocksPanel_MM(object sender, MouseEventArgs e) {

    }

    private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
      ListViewItem li;
      TopicM it=sender as TopicM;
      if((li= sender as ListViewItem)!=null && (it=li.DataContext as TopicM)!=null) {
        Workspace.This.AddFile(it);
      }
    }
    private void Window_Closing(object sender, CancelEventArgs e) {
      //if(_client!=null) {
      //  _client.Close();
      //  _client=null;
      //}
      var layoutSerializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
      try {
        var lDoc=new XmlDocument();
        using(var ix=lDoc.CreateNavigator().AppendChild()) {
          layoutSerializer.Serialize(ix);
        }

        var xd=new XmlDocument();
        var root=xd.CreateElement("Config");
        xd.AppendChild(root);
        //if(!string.IsNullOrWhiteSpace(_connectionUrl)) {
        //  var xUrl=xd.CreateElement("Url");
        //  xUrl.InnerText=_connectionUrl;
        //  root.AppendChild(xUrl);
        //}
        var window=xd.CreateElement("Window");
        {
          var tmp=xd.CreateAttribute("State");
          tmp.Value=this.WindowState.ToString();
          window.Attributes.Append(tmp);

          tmp=xd.CreateAttribute("Left");
          tmp.Value=this.Left.ToString();
          window.Attributes.Append(tmp);

          tmp=xd.CreateAttribute("Top");
          tmp.Value=this.Top.ToString();
          window.Attributes.Append(tmp);

          tmp=xd.CreateAttribute("Width");
          tmp.Value=this.Width.ToString();
          window.Attributes.Append(tmp);

          tmp=xd.CreateAttribute("Height");
          tmp.Value=this.Height.ToString();
          window.Attributes.Append(tmp);
        }
        root.AppendChild(window);
        root.AppendChild(xd.ImportNode(lDoc.FirstChild, true));
        xd.Save(_cfgPath);
      }
      catch(Exception ex) {
        Log.Error("Save config - {0}", ex.Message);
      }

    }

  }
}
