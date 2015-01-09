using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using X13.PLC;

namespace UnitTests.Core {
  [TestClass]
  public class UT_PI {
    [TestInitialize()]
    public void TestInitialize() {
      X13.PLC.PLC.instance.Clear();
      X13.PLC.PLC.instance.Tick();
    }

    [TestMethod]
    public void Test01() {
      
      var p1=Topic.root.Get("/plc1");
      var v1=p1.Get("v1");
      var v2=p1.Get("v2");
      var l1_v=new PiLink(v1, v2);
      var l1_t=p1.Get("w001");
      l1_t.value=l1_v;
      PLC.instance.Tick();
      PLC.instance.Start();
      Assert.AreEqual(1, l1_v.input.layer);
      Assert.AreEqual(1, l1_v.output.layer);
      string json=l1_t.ToJson();
      //Assert.AreEqual("{ \"i\" : \"/plc1/v1\", \"o\" : \"/plc1/v2\" }", json);
      var l2_t=p1.Get("w002");
      l2_t.SetJson(json);
      PLC.instance.Tick();
    }
  }
}
