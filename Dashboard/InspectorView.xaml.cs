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
  }
  class IVColorConverter : IValueConverter {
    private static int count;
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
      return (count++) % 2 == 0?Brushes.MintCream:Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
      throw new NotImplementedException();
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
  internal class InspectorValueTemplateSelector : DataTemplateSelector {
    public DataTemplate BoolValue { get; set; }
    public DataTemplate IntValue { get; set; }
    public DataTemplate DoubleValue { get; set; }
    public DataTemplate StringValue { get; set; }
    public DataTemplate OtherValue { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container) {
      ItemViewModel i=item as ItemViewModel;
      ValueVM m=(i==null)?item as ValueVM:i.ValueO;
      if(m==null){
        return null;
      }
      switch(m.ValueType) {
      case JSObjectType.Bool:
        return BoolValue;
      case JSObjectType.Int:
        return IntValue;
      case JSObjectType.Double:
        return DoubleValue;
      case JSObjectType.String:
        return StringValue;
      }
      return OtherValue;
    }
  }
}
