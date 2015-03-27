using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Windows.Threading;

namespace X13.model {
  class ViewModelBase : INotifyPropertyChanged {

    protected virtual void RaisePropertyChanged(string propertyName) {
      var eventHandler = PropertyChanged;
      if(eventHandler != null) {
        Delegate[] delegates = eventHandler.GetInvocationList();
        // Walk thru invocation list
        foreach(PropertyChangedEventHandler handler in delegates) {
          var dispatcherObject = handler.Target as DispatcherObject;
          // If the subscriber is a DispatcherObject and different thread
          if(dispatcherObject != null && dispatcherObject.CheckAccess() == false)
            // Invoke handler in the target dispatcher's thread
            dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, new PropertyChangedEventArgs(propertyName));
          else // Execute handler as is
            handler(this, new PropertyChangedEventArgs(propertyName));
        }
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;
  }
}
