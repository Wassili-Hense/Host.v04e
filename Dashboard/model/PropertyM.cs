using NiL.JS.Core;
using JST = NiL.JS.BaseLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using MIm=System.Windows.Media.Imaging;

namespace X13.model {
  internal class PropertyM : ViewModelBase {
    protected static SortedList<string, MIm.BitmapImage> _icons;

    static PropertyM() {
      _icons=new SortedList<string, MIm.BitmapImage>();
      _icons[ViewTypeEn.Bool]=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_bool.png", UriKind.Relative));
      _icons[ViewTypeEn.Int]=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_i64.png", UriKind.Relative));
      _icons[ViewTypeEn.Double]=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_f02.png", UriKind.Relative));
      _icons[ViewTypeEn.DateTime]=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_dt.png", UriKind.Relative));
      _icons[ViewTypeEn.String]=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_str.png", UriKind.Relative));
      _icons[ViewTypeEn.PiAlias]=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_ref.png", UriKind.Relative));
      _icons[ViewTypeEn.PiLink]=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_wire.png", UriKind.Relative));
      _icons[ViewTypeEn.Object]=new MIm.BitmapImage(new Uri("/Dashboard;component/Images/ty_obj.png", UriKind.Relative));
    }

    protected PropertyM _parent;
    protected JSObject _value;
    private string _oType;
    private string _viewType;
    private bool _isSelected;

    protected PropertyM(PropertyM parent, string name) {
      this.Name=name;
      _parent=parent;
      if(string.IsNullOrEmpty(Name)) {
        EditName=true;
      }
    }

    public ObservableCollection<PropertyM> Properties { get; private set; }
    public bool EditName { get; protected set; }
    public bool Expanded { get; set; }
    public bool IsSelected {
      get { return _isSelected; }
      set {
        if(value!=_isSelected) {
          _isSelected=value;
          RaisePropertyChanged("IsSelected");
        }
      }
    }
    public string Name { get; protected set; }
    public JSObjectType ValueType {
      get {
        return _value==null?JSObjectType.Undefined:_value.ValueType;
      }
    }
    public object Value {
      get {
        if(_value==null) {
          return null;
        }
        switch(ViewType) {
        case ViewTypeEn.Bool:
          return _value.As<bool>();
        case ViewTypeEn.Int:
          return _value.As<long>();
        case ViewTypeEn.Double:
          return _value.As<double>();
        case ViewTypeEn.DateTime:
          return _value.As<DateTime>();
        case ViewTypeEn.String:
          return _value.As<string>();
        case ViewTypeEn.PiAlias: {
            JSObject alias;
            if(_value.Value!=null && (alias=_value["alias"]).IsDefinded) {
              return alias.As<string>();
            }
          }
          goto default;
        case ViewTypeEn.PiLink: {
          JSObject i, o;
            if(_value.Value!=null && (i=_value["i"]).IsDefinded && (o=_value["o"]).IsDefinded) {
              return i.As<string>()+" ► "+o.As<string>();
            }
          }
          goto default;
        case ViewTypeEn.PiBlock: {
            JSObject func;
            if(_value.Value!=null && (func=_value["func"]).IsDefinded) {
              return func.As<string>();
            }
          }
          goto default;
        default:
          if(_value==null || _value==JSObject.Undefined) {
            return "undefined";
          }
          if(_value.ValueType==JSObjectType.Object) {
            JSObject otype;
            if(_value.Value==null) {
              return "null";
            } else if(_value.Value!=null && (otype=_value["$type"]).IsDefinded) {
              return otype.As<string>();
            } else {
              return "{ }";
            }
          }
          return _value.Value;
        }
      }
      set {
        JSObject val=null;
        switch(Type.GetTypeCode(value==null?null:value.GetType())) {
        case TypeCode.Boolean:
          val=new JST.Boolean((bool)value);
          break;
        case TypeCode.Byte:
        case TypeCode.SByte:
        case TypeCode.Int16:
        case TypeCode.Int32:
        case TypeCode.UInt16:
          val=new JST.Number(Convert.ToInt32(value));
          break;
        case TypeCode.Int64:
        case TypeCode.UInt32:
        case TypeCode.UInt64:
          val=new JST.Number(Convert.ToInt64(value));
          break;
        case TypeCode.Single:
        case TypeCode.Double:
        case TypeCode.Decimal:
          val=new JST.Number(Convert.ToDouble(value));
          break;
        case TypeCode.DateTime: {
            var dt = ((DateTime)value);
            var jdt=new JST.Date(dt);
            val=new JSObject(jdt);
          }
          break;
        case TypeCode.Empty:
          val=JSObject.Undefined;
          break;
        case TypeCode.String:
          val=new JST.String((string)value);
          break;
        case TypeCode.Object:
        default: {
            JSObject jo;
            if((jo = value as JSObject)!=null) {
              val=jo;
            } else {
              val=new JSObject(value);
            }
          }
          break;
        }
        if(val==null) {
          val=JSObject.Undefined;
        }
        _value=val;
        if(!(this is TopicM)) {
          _parent._value[Name]=val;
        }
        Publish();
        if(ViewType!=ViewTypeEn.Object && Properties!=null && Properties.Count>0) {
          Properties.Clear();
          this.RaisePropertyChanged("Properties");
        }
      }
    }
    public string ViewType {
      get {
        if(string.IsNullOrEmpty(_viewType)) {
          switch(_value==null?JSObjectType.Undefined:_value.ValueType) {
          case JSObjectType.Bool:
            _viewType=ViewTypeEn.Bool;
            break;
          case JSObjectType.Int:
            _viewType=ViewTypeEn.Int;
            break;
          case JSObjectType.Double:
            _viewType=ViewTypeEn.Double;
            break;
          case JSObjectType.String:
            _viewType=ViewTypeEn.String;
            break;
          case JSObjectType.Date:
            _viewType=ViewTypeEn.DateTime;
            break;
          case JSObjectType.Object: {
              JSObject otype;
              if(_value.Value!=null && (otype=_value["$type"]).IsDefinded) {
                switch(otype.As<string>()) {
                case "PiAlias":
                  _viewType=ViewTypeEn.PiAlias;
                  break;
                case "PiLink":
                  _viewType=ViewTypeEn.PiLink;
                  break;
                case "PiBlock":
                  _viewType=ViewTypeEn.PiBlock;
                  break;
                default:
                  _viewType=ViewTypeEn.Object;
                  break;
                }
              } else {
                _viewType=ViewTypeEn.Object;
              }
            }
            break;
          default:
            _viewType=ViewTypeEn.Object;
            break;
          }
        }
        return _viewType;
      }
      set {
        if(ViewTypeEn.Check(value)) {
          _viewType=value;
          this.RaisePropertyChanged("ViewType");
        }
      }
    }
    public MIm.BitmapImage Icon {
      get {
        MIm.BitmapImage ic;
        string vt=ViewType;
        if(vt==ViewTypeEn.PiBlock) {
          JSObject func;
          if(_value.Value!=null && (func=_value["func"]).IsDefinded) {
            vt=func.As<string>();
          }
        }
        if(!_icons.TryGetValue(vt, out ic)) {
          ic=_icons[ViewTypeEn.Object];
        }
        return ic;
      }
    }

    public void AddProperty() {
      bool pr=false;
      bool exp=false;
      if(Properties==null) {
        Properties=new ObservableCollection<PropertyM>();
        pr=true;
      }
      Properties.Insert(0, new PropertyM(this, string.Empty));
      if(!Expanded) {
        Expanded=true;
        exp=true;
      }
      if(pr) {
        this.RaisePropertyChanged("Properties");
      }
      if(exp) {
        this.RaisePropertyChanged("Expanded");
      }
    }
    public void StartRename() {
      EditName=true;
      RaisePropertyChanged("EditName");
    }
    public virtual void SetName(string nname) {
      if(!EditName) {
        return;
      }
      if(nname==null) {
        nname=this.Name;
      }
      if(string.IsNullOrEmpty(Name)) {
        if(string.IsNullOrEmpty(nname)) {
          this.Remove(false);
        } else {
          this.Name=nname;
          if(_parent._value.Value==null || _parent._value==JSObject.Undefined) {
            _parent._value=JST.JSON.parse("{ }");
          }
          _value=_parent._value.DefineMember(Name);
          int i, j;
          for(i=_parent.Properties.Count-1; i>=0; i--) {
            j=string.Compare(_parent.Properties[i].Name, this.Name);
            if(j==0) {
              i=-1-i;
              break;
            }
            if(j<0) {
              break;
            }
          }
          //i++;
          if(i>=0) {
            _parent.Properties.Move(0, i);
          }
        }
      } else {
        // rename property ???
      }
      EditName=false;
      RaisePropertyChanged("EditName");
      RaisePropertyChanged("Name");
    }
    public virtual void Remove(bool ext) {
      _parent._value[Name]=JSObject.Undefined;
      _parent.Properties.Remove(this);
      if(ext) {
        Publish();
      }
    }
    protected virtual void Publish() {
      if(_parent!=null) {
        _parent.Publish();
      }
    }
    protected void SetValue(JSObject value) {
      int i, j;
      bool propCh=false;
      PropertyM np=null;
      _value=value;
      _viewType=null;
      if(_value!=null && _value.ValueType>=JSObjectType.Object) {
        var names=_value.ToArray();
        if(Properties==null) {
          Properties=new ObservableCollection<PropertyM>();
          propCh=true;
        } else {
          for(i=Properties.Count-1; i>=0; i--) {
            if(names.All(z2 => z2!=Properties[i].Name)) {
              Properties.RemoveAt(i);
            }
          }
        }
        foreach(var n in names) {
          if(_parent is TopicM && n=="$type") {
            _oType=_value.GetMember("$type").As<string>();
          } else {
            for(i=Properties.Count-1; i>=0; i--) {
              j=string.Compare(Properties[i].Name, n);
              if(j==0) {
                np=Properties[i];
                i=-2-i;
                break;
              }
              if(j<0) {
                break;
              }
            }
            i++;
            if(i>=0) {
              np=new PropertyM(this, n);
              Properties.Insert(i, np);
            }
            np.SetValue(_value.GetMember(n));
          }
        }
      } else if(Properties!=null && Properties.Count!=0) {
        Properties.Clear();
      }

      this.RaisePropertyChanged("ViewType");
      this.RaisePropertyChanged("Value");
      if(propCh) {
        this.RaisePropertyChanged("Properties");
        propCh=true;
      }
    }
    public virtual string GetUri(string p) {
      string r;
      if(p==null) {
        r=string.Concat(_parent.GetUri(null), ".", Name);
      } else {
        r=string.Concat(_parent.GetUri(null), ".", Name, "?", p);
      }
      return r;
    }

    public override string ToString() {
      return GetUri(null);
    }
  }
  internal static class ViewTypeEn {
    private static string[] _arr=new string[] { Bool, Int, Double, DateTime, String, PiLink, PiAlias, PiBlock, Object };
    public const string Bool="bool";
    public const string Int="int";
    public const string Double="double";
    public const string DateTime="DateTime";
    public const string String="string";
    public const string PiLink="PLC.Link";
    public const string PiAlias="PLC.Alias";
    public const string PiBlock="PLC.Block";
    public const string Object="object";

    public static bool Check(string vt) {
      for(int i=0; i<_arr.Length; i++) {
        if(vt==_arr[i]) {
          return true;
        }
      }
      return false;
    }
  }
}
