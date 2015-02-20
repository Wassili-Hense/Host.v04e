using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using X13.model;
using Xceed.Wpf.AvalonDock.Layout;

namespace X13.UI {
  internal class PanesTemplateSelector : DataTemplateSelector {
    public PanesTemplateSelector() {

    }

    public DataTemplate InTemplate {
      get;
      set;
    }
    public DataTemplate LoTemplate {
      get;
      set;
    }

    public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container) {
      var it=item as TopicM;
      if(it!=null) {
        if(it.View==Projection.LO) {
          return LoTemplate;
        }
        if(it.View==Projection.IN) {
          return InTemplate;
        }
      }

      return base.SelectTemplate(item, container);
    }
  }
}
