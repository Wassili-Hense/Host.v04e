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
      if((p=sender as StackPanel)!=null) {
        var items=p.ContextMenu.Items;
        items.Add(new MenuItem() { Header="Add child" });
        items.Add(new MenuItem() { Header="View As" });
        items.Add(new MenuItem() { Header="Remove" });
      }

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
