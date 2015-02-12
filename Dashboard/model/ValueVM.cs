using NiL.JS.Core;
using NiL.JS.Core.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X13.model {
  internal class ValueVM : ViewModelBase {
    private ItemViewModel _item;
    private JSObject _parent;
    private JSObject _value;
    private string _name;

    public ValueVM(ItemViewModel item, string name, JSObject parent) {
      _item=item;
      _name=name;
      _parent=parent;
      if(_parent==null) {
        if(_name!="Alpha") {
          _value=JSON.parse("{ \"A\": 5, \"B\": 13.4, \"C\": { \"CA\" : \"test\", \"CB\" : null, \"CC\" : true } }");
        } else {
          _value=(DateTime.Now.Ticks&0x7FFF)/100.0;
        }
      } else {
        _value=_parent.GetMember(name);
      }
    }
    public IEnumerable<ValueVM> Properties {
      get {
        if(_value.ValueType>=JSObjectType.Object) {
          foreach(var n in _value) {
            yield return new ValueVM(_item, n, _value);
          }
        }
        yield break;
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
    public override string ToString() {
      return JSON.stringify(_value, null, null);
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
