using NiL.JS.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MIm=System.Windows.Media.Imaging;

namespace X13.model {
  internal class DeclarerM {
    public DeclarerM(string name) {
      this.Name=name;
    }
    public string Name { get; private set; }
    public string View { get; set; }
    public MIm.BitmapImage Icon { get; set; }

    public void Populate(JSObject value) {

    }
  }
  internal static class ViewTypeEn {
    public const string Bool="bool";
    public const string Int="int";
    public const string Double="double";
    public const string DateTime="DateTime";
    public const string String="string";
    public const string PiLink="PLC.Link";
    public const string PiAlias="PLC.Alias";
    public const string PiBlock="PLC.Block";
    public const string Object="object";
  }
}
