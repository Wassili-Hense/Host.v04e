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
        TopicM m;
        if(sp!=null && (m=sp.DataContext as TopicM)!=null) {
          e.Handled=true;
          Workspace.This.AddFile(m);
        }
      }
    }
    private void PathMouseLBU(object sender, MouseButtonEventArgs e) {
      var sp=sender as StackPanel;
      TopicM m;
      if(sp!=null && (m=sp.DataContext as TopicM)!=null) {
        e.Handled=true;
        Workspace.This.AddFile(m);
      }
      e.Handled=true;
    }


    private void StackPanel_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
      StackPanel p;
      MenuItem mi1, mi2;
      PropertyM v;

      if((p=sender as StackPanel)!=null && (v=p.DataContext as PropertyM)!=null) {
        var items=p.ContextMenu.Items;
        if(v.ViewType==ViewTypeEn.Object) {
          mi2=new MenuItem() { Header="Add child", Tag="A" };
          mi2.Click+=ContextMenuClick;
          items.Add(mi2);
        }

        mi1=new MenuItem() { Header="View As" };

        mi2=new MenuItem() { Header="Bool", Tag="#"+ViewTypeEn.Bool };
        mi2.Click+=ContextMenuClick;
        mi1.Items.Add(mi2);

        mi2=new MenuItem() { Header="Long", Tag="#"+ViewTypeEn.Int };
        mi2.Click+=ContextMenuClick;
        mi1.Items.Add(mi2);

        mi2=new MenuItem() { Header="Double", Tag="#"+ViewTypeEn.Double };
        mi2.Click+=ContextMenuClick;
        mi1.Items.Add(mi2);

        mi2=new MenuItem() { Header="DateTime", Tag="#"+ViewTypeEn.DateTime };
        mi2.Click+=ContextMenuClick;
        mi1.Items.Add(mi2);

        mi2=new MenuItem() { Header="String", Tag="#"+ViewTypeEn.String };
        mi2.Click+=ContextMenuClick;
        mi1.Items.Add(mi2);

        mi2=new MenuItem() { Header="Object", Tag="#"+ViewTypeEn.Object };
        mi2.Click+=ContextMenuClick;
        mi1.Items.Add(mi2);

        items.Add(mi1);

        mi2=new MenuItem() { Header="Rename", Tag="r" };
        mi2.Click+=ContextMenuClick;
        items.Add(mi2);

        mi2=new MenuItem() { Header="Remove", Tag="R" };
        mi2.Click+=ContextMenuClick;
        items.Add(mi2);
      }

    }

    void ContextMenuClick(object sender, RoutedEventArgs e) {
      MenuItem mi=sender as MenuItem;
      PropertyM v;
      string cmd;
      if(mi!=null && !string.IsNullOrEmpty(cmd=mi.Tag as string) && (v=mi.DataContext as PropertyM)!=null) {
        if(cmd[0]=='#') {
          v.ViewType=cmd.Substring(1);
        } else if(cmd[0]=='A') {
          v.AddProperty();
        }else if(cmd[0]=='r'){
          v.StartRename();
        } else if(cmd[0]=='R') {
          v.Remove(true);
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

    private void AddItemClick(object sender, RoutedEventArgs e) {
      TopicM it;
      var s=sender as Button;
      if(s!=null && (it=s.DataContext as TopicM)!=null) {
        //var z=
        it.AddChild();
        //var z1=this.tlInspector.ItemContainerGenerator.ContainerFromItem(z) as FrameworkElement;
        //this.tlInspector.Focus();
        //System.Reflection.MethodInfo selectMethod = typeof(TreeViewItem).GetMethod("Select", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        //selectMethod.Invoke(z1, new object[] { true });
      }
    }
    private void tbItemName_Loaded(object sender, RoutedEventArgs e) {
      tbItemName_FocusableChanged(sender, new DependencyPropertyChangedEventArgs() );
    }

    private void tbItemName_FocusableChanged(object sender, DependencyPropertyChangedEventArgs e) {
      var tb=sender as TextBox;
      PropertyM v;
      if(tb!=null && tb.Focusable && (v=tb.DataContext as PropertyM)!=null && v.EditName) {
        if(!string.IsNullOrEmpty(tb.Text)) {
          tb.SelectAll();
        }
        tb.Focus();
      }
    }

    private void tbItemName_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
      var tb=sender as TextBox;
      PropertyM v;
      if(tb!=null && (v=tb.DataContext as PropertyM)!=null && v.EditName) {
        v.SetName(null);
      }
    }

    private void tbItemName_PreviewKeyDown(object sender, KeyEventArgs e) {
      TextBox tb;
      if((tb=sender as TextBox)==null) {
        return;
      }
      if(e.Key==Key.Escape) {
        tbItemName_LostKeyboardFocus(sender, null);
        e.Handled=true;
      } else if(e.Key==Key.Enter) {
        PropertyM v;
        try {
          if((v=tb.DataContext as PropertyM)!=null && v.EditName) {
            v.SetName(tb.Text);
          }
        }
        catch(ArgumentException ex) {
          X13.lib.Log.Error(ex.Message);
        }
        e.Handled=true;
      }
    }
  }
  internal class GridColumnSpringConverter : IMultiValueConverter {
    public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) {
      double ret=values.Cast<double>().Aggregate((x, y) => x -= y) - 26;
      return ret>30?ret:30;
    }
    public object[] ConvertBack(object value, System.Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) {
      throw new System.NotImplementedException();
    }
  }
}
