using NiL.JS.Core;
using NiL.JS.Core.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace X13.model {
  internal class ValueVM : ViewModelBase {
    private ItemViewModel _item;
    private ValueVM _parent;
    internal JSObject _value;
    private string _name;
    private ObservableCollection<ValueVM> _properies;
    private string _oType;
    private string _viewType;

    private ValueVM(ItemViewModel item, JSObject value, string name, ValueVM parent) {
      _item=item;
      _name=name;
      _parent=parent;
      _value=value;
      if(_value!=null && _value.ValueType>=JSObjectType.Object) {
        _properies=new ObservableCollection<ValueVM>();
        foreach(var n in _value) {
          if(name==null && n=="$type") {
            _oType=_value.GetMember("$type").As<string>();
          } else {
            _properies.Add(new ValueVM(_item, this._value.GetMember(n), n, this));
          }
        }
      }
    }
    public ValueVM(ItemViewModel item, JSObject value)
      : this(item, value??JSObject.Undefined, null, null) {
    }
    public ObservableCollection<ValueVM> Properties {
      get {
        return _properies;
      }
    }
    public string Name { get { return _name; } }
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
        UpdateValue(value);
      }
    }
    public JSObjectType ValueType {
      get {
        return _value==null?JSObjectType.Undefined:_value.ValueType;
      }
    }
    public string ViewType {
      get {
        if(string.IsNullOrEmpty(_viewType)) {
          switch(this.ValueType) {
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
    public override string ToString() {
      return _oType??(_value==null?"null":_value.ToString());
    }
    public void Remove() {
      if(_parent!=null) {
        _parent._value[_name]=JSObject.Undefined;
        _parent._properies.Remove(this);
        _item.Update();
      } else {
        _value=JSObject.Undefined;
        _item.Remove();
      }
    }

    private bool UpdateValue(object o) {
      JSObject val=null;
      switch(Type.GetTypeCode(o==null?null:o.GetType())) {
      case TypeCode.Boolean:
        val=new NiL.JS.Core.BaseTypes.Boolean((bool)o);
        break;
      case TypeCode.Byte:
      case TypeCode.SByte:
      case TypeCode.Int16:
      case TypeCode.Int32:
      case TypeCode.UInt16:
        val=new NiL.JS.Core.BaseTypes.Number(Convert.ToInt32(o));
        break;
      case TypeCode.Int64:
      case TypeCode.UInt32:
      case TypeCode.UInt64:
        val=new NiL.JS.Core.BaseTypes.Number(Convert.ToInt64(o));
        break;
      case TypeCode.Single:
      case TypeCode.Double:
      case TypeCode.Decimal:
        val=new NiL.JS.Core.BaseTypes.Number(Convert.ToDouble(o));
        break;
      case TypeCode.DateTime: {
          var dt = ((DateTime)o);
          var jdt=new NiL.JS.Core.BaseTypes.Date(new NiL.JS.Core.Arguments { dt.Year, dt.Month, dt.Year, dt.Hour, dt.Minute, dt.Second, dt.Millisecond });
          val=jdt.getTime();
        }
        break;
      case TypeCode.Empty:
        val=JSObject.Undefined;
        break;
      case TypeCode.String:
        val=new NiL.JS.Core.BaseTypes.String((string)o);
        break;
      case TypeCode.Object:
      default: {
          JSObject jo;
          if((jo = o as JSObject)!=null) {
            val=jo;
          } else {
            val=new JSObject(o);
          }
        }
        break;
      }
      if(val==null) {
        val=JSObject.Undefined;
      }
      _value=val;
      if(_parent!=null) {
        _parent._value[_name]=val;
      }
      _item.Update();
      return true;
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
