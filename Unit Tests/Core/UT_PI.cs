﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using X13.PLC;
using NiL.JS.Core;

namespace UnitTests.Core {
  [TestClass]
  public class UT_PI {
    [TestInitialize()]
    public void TestInitialize() {
      PLC.instance.Init();
      PLC.instance.Clear();
      PLC.instance.Tick();
    }

    [TestMethod]
    public void Test01() {
      
      var p=Topic.root.Get("/plc1");
      var v1=p.Get("v1");
      var v2=p.Get("v2");
      var l1_v=new PiLink(v1, v2);
      var l1_t=p.Get("w001");
      l1_t.value=l1_v;
      PLC.instance.Tick();
      PLC.instance.Start();
      Assert.AreEqual(1, l1_v.input.layer);
      Assert.AreEqual(1, l1_v.output.layer);
      string json=l1_t.ToJson();
      Assert.AreEqual("{\"i\":\"/plc1/v1\",\"o\":\"/plc1/v2\"}", json);
      v1.Set(new NiL.JS.Core.BaseTypes.Number(5));
      PLC.instance.Tick();
      Assert.AreEqual(5, v2.As<int>());
      //var l2_t=p.Get("w002");
      //l2_t.SetJson(json);
      //PLC.instance.Tick();

    }
    [TestMethod]
    public void Test02() {
      var p = Topic.root.Get("/plc2");
      var v1 = p.Get("v1");
      v1.Set(new NiL.JS.Core.BaseTypes.Number(3));
      var v2 = p.Get("v2");
      var a1 = new PiBlock("ADD");
      var a1_t = p.Get("A01");
      a1_t.value = a1;
      var a1_a = a1_t.Get("A");
      var w1 = new PiLink(v1, a1_a);
      var w1_t = p.Get("w001");
      w1_t.value = w1;
      var a1_q_t=a1_t.Get("Q");
      PLC.instance.Tick();
      PLC.instance.Tick();
      PLC.instance.Start();
      Assert.AreEqual(1, w1.input.layer);
      Assert.AreEqual(1, w1.output.layer);
      Assert.AreEqual(1, a1._pins["A"].layer);
      Assert.AreEqual(2, a1.layer);
      Assert.AreEqual(2, a1._pins["Q"].layer);
      Assert.AreEqual(4, a1_q_t.As<int>());
    }
  }
}
