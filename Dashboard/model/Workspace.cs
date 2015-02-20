using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace X13.model {
  class Workspace : ViewModelBase {
    #region static
    static Workspace() {
      _this = new Workspace();
    }

    static Workspace _this;
    public static Workspace This {
      get { return _this; }
    }
    #endregion static

    #region instance

    #region instance variables

    private ObservableCollection<TopicM> _files;
    private ReadOnlyObservableCollection<TopicM> _readonyFiles;

    #endregion instance variables

    private Workspace() {
      _files = new ObservableCollection<TopicM>();
      _readonyFiles = null;
      _files.Add(TopicM.root);
    }


    public ReadOnlyObservableCollection<TopicM> Files {
      get {
        if(_readonyFiles == null)
          _readonyFiles = new ReadOnlyObservableCollection<TopicM>(_files);

        return _readonyFiles;
      }
    }
    public void AddFile(TopicM i) {
      if(_files.All(z => z!=i)) {
        _files.Add(i);
      }
      ActiveDocument=i;
    }
    public void CloseFile(TopicM i) {
      _files.Remove(i);
    }
    private TopicM _activeDocument = null;
    public TopicM ActiveDocument {
      get { return _activeDocument; }
      set {
        if(_activeDocument != value) {
          _activeDocument = value;
          RaisePropertyChanged("ActiveDocument");
        }
      }
    }

    #endregion instance

    public ViewModelBase Open(string p) {
      if(p==null || p.Length<3) {
        return null;
      }
      var fileViewModel = _files.FirstOrDefault(fm => fm.ContentId == p);
      if(fileViewModel != null) {
        this.ActiveDocument = fileViewModel; // File is already open so shiw it

        return fileViewModel;
      }

      fileViewModel = _files.FirstOrDefault(fm => fm.ContentId == p);
      if(fileViewModel != null)
        return fileViewModel;
      if(p.StartsWith("LO:")) {
        var r=TopicM.root.Get(p.Substring(3));
        r.View=Projection.LO;
        _files.Add(r);
        return r;
      } else if(p.StartsWith("IN:")) {
        var r=TopicM.root.Get(p.Substring(3));
        r.View=Projection.IN;
        _files.Add(r);
        return r;
      }
      return null;
    }
  }
}
