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
    private JSObject _parent;
    internal JSObject _value;
    private string _name;
    private List<ValueVM> _properies;
    private string _oType;
    private string _viewType;

    public ValueVM(ItemViewModel item, string name, JSObject value) {
      _item=item;
      if(name==null) {
        _name=null;
        _parent=null;
        _value=value;
      } else {
        _name=name;
        _parent=value;
        _value=_parent.GetMember(name);
      }
      if(_value!=null && _value.ValueType>=JSObjectType.Object) {
        _properies=new List<ValueVM>();
        foreach(var n in _value) {
          if(name==null && n=="$type") {
            _oType=_value.GetMember("$type").As<string>();
          } else {
            _properies.Add(new ValueVM(_item, n, _value));
          }
        }
      }
    }
    public List<ValueVM> Properties {
      get {
        return _properies;
      }
    }
    public string Name { get { return _name; } }
    public object Value {
      get {
        return _value==null?null:_value.Value;
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
            _viewType="bool";
            break;
          case JSObjectType.Int:
            _viewType="int";
            break;
          case JSObjectType.Double:
            _viewType="double";
            break;
          case JSObjectType.String:
            _viewType="string";
            break;
          case JSObjectType.Date:
            _viewType="DateTime";
            break;
          default:
            _viewType="other";
            break;
          }
        }
        return _viewType;
      }
      set {
      }
    }
    public override string ToString() {
      return _oType??(_value==null?"null":_value.ToString());
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
        _parent[_name]=val;
      }
      _item.Update();
      return true;
    }
  }
}
