using NiL.JS.Core;
using System;
using System.Collections.Generic;
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
using X13.model;

namespace X13.UI {
  /// <summary>
  /// Interaktionslogik für InspectorView.xaml
  /// </summary>
  public partial class InspectorView : UserControl {
    public InspectorView() {
      InitializeComponent();
    }

    private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      if(e.ClickCount==2) {
        var sp=sender as Grid;
        ItemViewModel m;
        if(sp!=null && (m=sp.DataContext as ItemViewModel)!=null) {
          e.Handled=true;
          Workspace.This.AddFile(m);
        }
      }
    }

    private void StackPanel_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
      StackPanel p;
      MenuItem mi;
      if((p=sender as StackPanel)!=null) {
        var items=p.ContextMenu.Items;
        mi=new MenuItem() { Header="Add child", Tag="A" };
        mi.Click+=ContextMenuClick;
        items.Add(mi);

        var va=new MenuItem() { Header="View As" };

        mi=new MenuItem() { Header="Bool", Tag="#"+ViewTypeEn.Bool };
        mi.Click+=ContextMenuClick;
        va.Items.Add(mi);

        mi=new MenuItem() { Header="Long", Tag="#"+ViewTypeEn.Int };
        mi.Click+=ContextMenuClick;
        va.Items.Add(mi);

        mi=new MenuItem() { Header="Double", Tag="#"+ViewTypeEn.Double };
        mi.Click+=ContextMenuClick;
        va.Items.Add(mi);

        mi=new MenuItem() { Header="DateTime", Tag="#"+ViewTypeEn.DateTime };
        mi.Click+=ContextMenuClick;
        va.Items.Add(mi);

        mi=new MenuItem() { Header="String", Tag="#"+ViewTypeEn.String };
        mi.Click+=ContextMenuClick;
        va.Items.Add(mi);

        mi=new MenuItem() { Header="Object", Tag="#"+ViewTypeEn.Object };
        mi.Click+=ContextMenuClick;
        va.Items.Add(mi);

        items.Add(va);
        mi=new MenuItem() { Header="Remove", Tag="R" };
        mi.Click+=ContextMenuClick;
        items.Add(mi);
      }

    }

    void ContextMenuClick(object sender, RoutedEventArgs e) {
      MenuItem mi=sender as MenuItem;
      ItemViewModel it;
      ValueVM v;
      string cmd;
      if(mi!=null && !string.IsNullOrEmpty(cmd=mi.Tag as string) && ((v=mi.DataContext as ValueVM)!=null || ((it=mi.DataContext as ItemViewModel)!=null && (v=it.ValueO)!=null))) {
        if(cmd[0]=='#') {
          v.ViewType=cmd.Substring(1);
        } else if(cmd[0]=='R') {
          v.Remove();
        }
      }
      e.Handled=true;
    }

    private void StackPanel_ContextMenuClosing(object sender, ContextMenuEventArgs e) {
      StackPanel p;
      if((p=sender as StackPanel)!=null) {
        var items=p.ContextMenu.Items;
        items.Clear();
      }
    }

    private void Image_MouseUp(object sender, MouseButtonEventArgs e) {
      Image p;
      if(e.ClickCount==1 && e.ChangedButton==MouseButton.Left && (p=sender as Image)!=null) {
        var c=p;
        //TODO: show contextmenu
      }
    }
  }
  internal class GridColumnSpringConverter : IMultiValueConverter {
    public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) {
      return values.Cast<double>().Aggregate((x, y) => x -= y) - 26;
    }
    public object[] ConvertBack(object value, System.Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) {
      throw new System.NotImplementedException();
    }
  }
}
