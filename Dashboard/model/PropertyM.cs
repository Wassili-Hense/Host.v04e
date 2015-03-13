using NiL.JS.Core;
using NiL.JS.Core.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace X13.model {
  internal class PropertyM : ViewModelBase {
    protected PropertyM _parent;
    protected JSObject _value;
    private string _oType;
    private string _viewType;

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
        default:
          return _value.Value;
        }
      }
      set {
        JSObject val=null;
        switch(Type.GetTypeCode(value==null?null:value.GetType())) {
        case TypeCode.Boolean:
          val=new NiL.JS.Core.BaseTypes.Boolean((bool)value);
          break;
        case TypeCode.Byte:
        case TypeCode.SByte:
        case TypeCode.Int16:
        case TypeCode.Int32:
        case TypeCode.UInt16:
          val=new NiL.JS.Core.BaseTypes.Number(Convert.ToInt32(value));
          break;
        case TypeCode.Int64:
        case TypeCode.UInt32:
        case TypeCode.UInt64:
          val=new NiL.JS.Core.BaseTypes.Number(Convert.ToInt64(value));
          break;
        case TypeCode.Single:
        case TypeCode.Double:
        case TypeCode.Decimal:
          val=new NiL.JS.Core.BaseTypes.Number(Convert.ToDouble(value));
          break;
        case TypeCode.DateTime: {
            var dt = ((DateTime)value);
            var jdt=new NiL.JS.Core.BaseTypes.Date(new NiL.JS.Core.Arguments { dt.Year, dt.Month-1, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond });
            val=new JSObject(jdt);
          }
          break;
        case TypeCode.Empty:
          val=JSObject.Undefined;
          break;
        case TypeCode.String:
          val=new NiL.JS.Core.BaseTypes.String((string)value);
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
            _parent._value=JSON.parse("{ }");
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

      }
      EditName=false;
      RaisePropertyChanged("EditName");
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

    public override string ToString() {
      return _parent.ToString()+"."+Name;
    }
  }
  internal static class ViewTypeEn {
    private static string[] _arr=new string[] { Bool, Int, Double, DateTime, String, Object };
    public const string Bool="bool";
    public const string Int="int";
    public const string Double="double";
    public const string DateTime="DateTime";
    public const string String="string";
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
