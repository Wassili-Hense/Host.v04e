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

    private void PathMouseLRBU(object sender, MouseButtonEventArgs e) {
      var sp=sender as FrameworkElement;
      TopicM m;
      if(sp!=null && (m=sp.DataContext as TopicM)!=null) {
        if(this.DataContext!=m) {
          Workspace.This.AddFile(m);
        } else {
          FillContectMenu(sp, m, true);
          if(sp.ContextMenu!=null && sp.ContextMenu.Items.Count>0) {
            sp.ContextMenu.IsOpen=true;
          }
        }
      }
      e.Handled=true;
    }
    private void ItemMouseUp(object sender, MouseButtonEventArgs e) {
      var sp=sender as FrameworkElement;
      PropertyM m;
      if(e.ClickCount==1 && e.ChangedButton==MouseButton.Right && sp!=null  && (m=sp.DataContext as PropertyM)!=null) {
        m.IsSelected=true;
        FillContectMenu(sp, m, false);
        if(sp.ContextMenu!=null && sp.ContextMenu.Items.Count>0) {
          sp.ContextMenu.IsOpen=true;
        }
        e.Handled=true;
      }
    }
    private void Image_MouseUp(object sender, MouseButtonEventArgs e) {
      var sp=sender as FrameworkElement;
      PropertyM m;
      if(e.ClickCount==1 && e.ChangedButton==MouseButton.Left && sp!=null  && (m=sp.DataContext as PropertyM)!=null) {
        m.IsSelected=true;
        FillContectMenu(sp, m, false);
        if(sp.ContextMenu!=null && sp.ContextMenu.Items.Count>0) {
          sp.ContextMenu.IsOpen=true;
        }
        e.Handled=true;
      }
    }

    void ContextMenuClick(object sender, RoutedEventArgs e) {
      MenuItem mi=sender as MenuItem;
      PropertyM v;
      string cmd;
      if(mi!=null && !string.IsNullOrEmpty(cmd=mi.Tag as string) && (v=mi.DataContext as PropertyM)!=null) {
        switch(cmd[0]) {
        case '#':
          v.ViewType=cmd.Substring(1);
          break;
        case 'A':
          v.AddProperty();
          break;
        case 'a':
          //var z=
          (v as TopicM).AddChild();
          //var z1=this.tlInspector.ItemContainerGenerator.ContainerFromItem(z) as FrameworkElement;
          //this.tlInspector.Focus();
          //System.Reflection.MethodInfo selectMethod = typeof(TreeViewItem).GetMethod("Select", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
          //selectMethod.Invoke(z1, new object[] { true });
          break;
        case 'C':  // Cut
          Clipboard.Clear();
          Clipboard.SetText(v.GetUri("move"), TextDataFormat.UnicodeText);
          break;
        case 'c': // Copy
          Clipboard.Clear();
          Clipboard.SetText(v.GetUri("copy"), TextDataFormat.UnicodeText);
          break;
        case 'r':
          v.StartRename();
          break;
        case 'P':  // Paste
          if(Clipboard.ContainsText(TextDataFormat.UnicodeText)) {
            try {
              Uri r;
              Uri.TryCreate(Clipboard.GetText(TextDataFormat.UnicodeText), UriKind.Absolute, out r);
              TopicM src;
              TopicM par;
              if(r.Scheme=="x13" && (src=TopicM.root.Get(r.AbsolutePath, false))!=null && (par=v as TopicM)!=null) {
                if(r.Query=="?copy") {
                  WsClient.instance.Copy(src.Path, par.Path, src.Name);
                } else if(r.Query=="?move") {
                  WsClient.instance.Move(src.Path, par.Path, src.Name);
                }
              }
            }
            catch(Exception ex) {
              X13.lib.Log.Debug("{0}.Paste({1}) - {2}", v.ToString(), Clipboard.GetText(), ex.Message);
            }
          }
          break;
        case 'R':
          v.Remove(true);
          break;
        }
      }
      e.Handled=true;
    }

    private void StackPanel_ContextMenuClosing(object sender, ContextMenuEventArgs e) {
      var p=sender as FrameworkElement;
      if(p!=null) {
        var items=p.ContextMenu.Items;
        items.Clear();
      }
    }

    private void FillContectMenu(FrameworkElement s, PropertyM v, bool header) {
      MenuItem mi1, mi2;
      bool isTopic=v is TopicM;
      bool sep=false;

      if(s.ContextMenu==null) {
        s.ContextMenu=new ContextMenu();
        s.ContextMenu.ContextMenuClosing+=StackPanel_ContextMenuClosing;
      } else {
        s.ContextMenu.Items.Clear();
      }
      s.ContextMenu.DataContext=v;

      var items=s.ContextMenu.Items;

      if(header) {
        mi1=new MenuItem() { Header="Add", Tag="a" };
        mi1.Click+=ContextMenuClick;
        items.Add(mi1);
      } else {
        if(v.ViewType==ViewTypeEn.Object) {
          mi1=new MenuItem() { Header="Add property", Tag="A" };
          mi1.Click+=ContextMenuClick;
          items.Add(mi1);
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
      }
      if(isTopic) {
        if(!header) {
          items.Add(new Separator());
          sep=true;

          mi1=new MenuItem() { Header="Cut", Tag="C" };
          mi1.Click+=ContextMenuClick;
          items.Add(mi1);

          mi1=new MenuItem() { Header="Copy", Tag="c" };
          mi1.Click+=ContextMenuClick;
          items.Add(mi1);
        }
        if(Clipboard.ContainsText(TextDataFormat.UnicodeText) && Clipboard.GetText(TextDataFormat.UnicodeText).StartsWith("x13://")) {
          if(!sep) {
            items.Add(new Separator());
            sep=true;
          }
          mi1=new MenuItem() { Header="Paste", Tag="P" };
          mi1.Click+=ContextMenuClick;
          items.Add(mi1);
        }
      }

      sep=false;

      if(!isTopic || !(v as TopicM).IsRoot) {
        items.Add(new Separator());
        sep=true;

        mi1=new MenuItem() { Header="Delete", Tag="R" };
        mi1.Click+=ContextMenuClick;
        items.Add(mi1);
      }

      if(isTopic && !header) {
        if(!sep) {
          items.Add(new Separator());
          sep=true;
        }
        mi1=new MenuItem() { Header="Rename", Tag="r" };
        mi1.Click+=ContextMenuClick;
        items.Add(mi1);
      }
    }

    private void ItemNameMouseLBD(object sender, MouseButtonEventArgs e) {
      if(e.ClickCount==2) {
        var sp=sender as FrameworkElement;
        TopicM m;
        if(sp!=null && (m=sp.DataContext as TopicM)!=null) {
          e.Handled=true;
          Workspace.This.AddFile(m);
        }
      }
    }

    private void tbItemName_Loaded(object sender, RoutedEventArgs e) {
      tbItemName_FocusableChanged(sender, new DependencyPropertyChangedEventArgs());
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
