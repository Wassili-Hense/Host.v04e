using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using X13.PLC;
using NiL.JS.Core;

namespace UnitTests.Core {
  [TestClass]
  public class UT_PI {
    private static Topic root;
    private static Random r;
    [ClassInitialize()]
    public static void MyClassInitialize(TestContext testContext) {
      r=new Random((int)DateTime.Now.Ticks);
      root=Topic.root;
    }

    [TestInitialize()]
    public void TestInitialize() {
      PLC.instance.Init();
      PLC.instance.Tick();
    }

    [TestMethod]
    public void T01() {
      Topic A1=root.Get("A1");
      Assert.AreEqual(root, A1.parent);
      Assert.AreEqual("A1", A1.name);
      Assert.AreEqual("/A1", A1.path);
    }
    [TestMethod]
    public void T02() {
      Topic A1=root.Get("A1");
      long val=r.Next();
      A1.Set(val);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(val, A1.As<long>());
    }
    [TestMethod]
    public void T04() {   // parse to bool
      Topic A1=root.Get("A1");
      A1.Set(true);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(true, A1.As<bool>());
      A1.Set(false);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(false, A1.As<bool>());
      A1.Set((object)true);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(true, A1.As<bool>());
      A1.Set(0);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(false, A1.As<bool>());
      A1.Set(r.Next(1, int.MaxValue));
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(true, A1.As<bool>());
      //A1.Set("false");
      //X13.PLC.PLC.instance.Tick();
      //Assert.AreEqual(false, A1.As<bool>());
      //A1.Set("True");
      //X13.PLC.PLC.instance.Tick();
      //Assert.AreEqual(false, A1.As<bool>());
    }
    [TestMethod]
    public void T05() {   // parse to long
      Topic A1=root.Get("A1");
      A1.Set((object)257);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(257, A1.As<int>());
      A1.Set(25.7);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(25, A1.As<long>());
      //A1.Set("94");
      //X13.PLC.PLC.instance.Tick();
      //Assert.AreEqual(94, A1.As<long>());
      A1.Set("0x15");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(0, A1.As<long>());
      //A1.Set("17.6");
      //X13.PLC.PLC.instance.Tick();
      //Assert.AreEqual(17, A1.As<long>());
      //A1.Set(true);
      //X13.PLC.PLC.instance.Tick();
      //Assert.AreEqual(1, A1.As<long>());
      //A1.Set(new DateTime(917L));
      //X13.PLC.PLC.instance.Tick();
      //Assert.AreEqual(917, A1.As<long>());
    }
    [TestMethod]
    public void T06() {   // parse to double
      Topic A1=root.Get("A1");
      A1.Set((object)257.158);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(257.158, A1.As<double>());
      A1.Set(52);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(52.0, A1.As<double>());
      //A1.Set("913");
      //X13.PLC.PLC.instance.Tick();
      //Assert.AreEqual(913.0, A1.As<double>());
      A1.Set("0x15");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(0.0, A1.As<double>());
      //A1.Set("294.3187");
      //X13.PLC.PLC.instance.Tick();
      //Assert.AreEqual(294.3187, A1.As<double>());
      //A1.Set(true);
      //X13.PLC.PLC.instance.Tick();
      //Assert.AreEqual(1.0, A1.As<double>());
      //A1.Set(DateTime.FromOADate(1638.324));
      //X13.PLC.PLC.instance.Tick();
      //Assert.AreEqual(1638.324, A1.As<double>());
    }
    [TestMethod]
    public void T07() {
      Topic A3=root.Get("A3");
      long val=r.Next();
      A3.Set(val);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(val, A3.As<long>());
      A3.Remove();
      A3.Set(Math.PI);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(true, A3.disposed);
      Assert.AreEqual(null, A3.As<object>());
    }
    [TestMethod]
    public void T08() {
      Topic t0=root.Get("child");
      var arr=t0.children.ToArray();
      Assert.AreEqual(0, arr.Length);
      var t1=t0.Get("ch_a");
      arr=t0.children.ToArray();
      Assert.AreEqual(1, arr.Length);
      Assert.AreEqual(t1, arr[0]);
      t1=t0.Get("ch_b");
      var t2=t1.Get("a");
      t2=t1.Get("b");
      t1=t0.Get("ch_c");
      t2=t1.Get("a");
      arr=t0.children.ToArray();
      Assert.AreEqual(3, arr.Length);
      arr=t0.all.ToArray();
      Assert.AreEqual(7, arr.Length);  // child, ch_a, ch_b, ch_b/a, ch_b/b, ch_c, ch_c/a
      Assert.AreEqual(t2, arr[6]);
      Assert.AreEqual(t1, arr[5]);
      Assert.AreEqual(t0, arr[0]);
    }
    [TestMethod]
    public void T09() {
      var cmds1=new List<Perform>();
      var dl=new Action<Topic, Perform>((s, p) => { cmds1.Add(p); });

      Topic t0=root.Get("child1");
      X13.PLC.PLC.instance.Tick();

      t0.changed+=dl;
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(t0, cmds1[0].src);
      Assert.AreEqual(Perform.Art.subscribe, cmds1[0].art);
      cmds1.Clear();
      var t1=t0.Get("ch_a");
      t1.Set("Hi");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(0, cmds1.Count);
      cmds1.Clear();
      t0.changed-=dl;
      t0.Set(2.98);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(0, cmds1.Count);
    }
    [TestMethod]
    public void T11() {
      var cmds1=new List<Perform>();
      var dl=new Action<Topic, Perform>((s, p) => { cmds1.Add(p); });

      Topic t0=root.Get("child2");
      var t1=t0.Get("ch_a");
      var t1_a=t1.Get("a");
      X13.PLC.PLC.instance.Tick();
      t0.children.changed+=dl;
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(Perform.Art.subscribe, cmds1[0].art);
      Assert.AreEqual(t1, cmds1[0].src);
      cmds1.Clear();

      t1.Set("Hi");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(Perform.Art.changed, cmds1[0].art);
      Assert.AreEqual(t1, cmds1[0].src);
      cmds1.Clear();

      var t2=t0.Get("ch_b");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(Perform.Art.create, cmds1[0].art);
      Assert.AreEqual(t2, cmds1[0].src);
      cmds1.Clear();

      var t2_a=t2.Get("a");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(0, cmds1.Count);
    }
    [TestMethod]
    public void T12() {
      var cmds1=new List<Perform>();
      var dl=new Action<Topic, Perform>((s, p) => { cmds1.Add(p); });

      Topic t0=root.Get("child3");
      var t1=t0.Get("ch_a");
      var t1_a=t1.Get("a");
      X13.PLC.PLC.instance.Tick();
      t0.all.changed+=dl;
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(3, cmds1.Count);
      Assert.AreEqual(Perform.Art.subscribe, cmds1[0].art);
      Assert.AreEqual(t0, cmds1[0].src);
      Assert.AreEqual(Perform.Art.subscribe, cmds1[1].art);
      Assert.AreEqual(t1, cmds1[1].src);
      Assert.AreEqual(Perform.Art.subscribe, cmds1[2].art);
      Assert.AreEqual(t1_a, cmds1[2].src);
      cmds1.Clear();

      t1.Set("Hi");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(Perform.Art.changed, cmds1[0].art);
      Assert.AreEqual(t1, cmds1[0].src);
      cmds1.Clear();

      var t2=t0.Get("ch_b");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(Perform.Art.create, cmds1[0].art);
      Assert.AreEqual(t2, cmds1[0].src);
      cmds1.Clear();

      var t2_a=t2.Get("a");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(Perform.Art.create, cmds1[0].art);
      Assert.AreEqual(t2_a, cmds1[0].src);
      cmds1.Clear();
    }
    [TestMethod]
    public void T13() {
      var b1=root.Get("B1");
      X13.PLC.PLC.instance.Tick();
      b1.Remove();
      X13.PLC.PLC.instance.Tick();
      Assert.IsTrue(b1.disposed);
      Assert.IsFalse(root.Exist("B1"));
      b1=null;
      var b2=root.Get("B2");
      var b2_a=b2.Get("A");
      X13.PLC.PLC.instance.Tick();
      b2.Remove();
      X13.PLC.PLC.instance.Tick();
      Assert.IsTrue(b2.disposed);
      Assert.IsFalse(root.Exist("B2"));
      Assert.IsTrue(b2_a.disposed);
      Assert.IsFalse(root.Exist("/B2/A"));

    }
    [TestMethod]
    public void T14() {
      var cmds1=new List<Perform>();
      var dl=new Action<Topic, Perform>((s, p) => { cmds1.Add(p); });

      var b3=root.Get("B3");
      X13.PLC.PLC.instance.Tick();
      b3.all.changed+=dl;
      b3.Set(91.02);
      X13.PLC.PLC.instance.Tick();
      cmds1.Clear();

      var c3=b3.Move(root, "C3");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(true, b3.disposed);
      Assert.AreEqual(false, root.Exist("B3"));
      Assert.AreNotEqual(b3, c3);
      Assert.AreEqual("C3", c3.name);
      Assert.AreEqual(91.02, c3.As<double>());
      Assert.AreEqual(2, cmds1.Count);
      Assert.AreEqual(b3, cmds1[0].src);
      Assert.AreEqual(Perform.Art.move, cmds1[0].art);
      Assert.AreEqual(c3, cmds1[1].src);
      Assert.AreEqual(Perform.Art.create, cmds1[1].art);
      cmds1.Clear();

      var c3_a=c3.Get("A");
      c3_a.Set(9577);
      X13.PLC.PLC.instance.Tick();
      cmds1.Clear();

      var d3=c3.Move(root, "D3");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(true, c3.disposed);
      Assert.AreEqual(false, root.Exist("C3"));
      Assert.AreNotEqual(c3, d3);
      Assert.AreEqual("D3", d3.name);
      Assert.AreEqual(91.02, d3.As<double>());
      Assert.AreEqual(d3, c3_a.parent);
      Assert.AreEqual("/D3/A", c3_a.path);
      Assert.AreEqual(9577, c3_a.As<long>());
      Assert.AreEqual(3, cmds1.Count);
      Assert.AreEqual(c3, cmds1[0].src);
      Assert.AreEqual(Perform.Art.move, cmds1[0].art);
      Assert.AreEqual(d3, cmds1[1].src);
      Assert.AreEqual(Perform.Art.create, cmds1[1].art);
      Assert.AreEqual(c3_a, cmds1[2].src);
      Assert.AreEqual(Perform.Art.create, cmds1[2].art);
      cmds1.Clear();

      d3.Set(17);
      var e3=d3.Move(root, "e3");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(17, e3.As<long>());
      cmds1.Clear();
    }
    [TestMethod]
    public void T20() {
      var p=Topic.root.Get("/plc1");
      var v1=p.Get("v1");
      var v2=p.Get("v2");
      var l1_v=new PiLink(v1, v2);
      var l1_t=p.Get("w001");
      l1_t.value=l1_v;
      v1.Set(5);
      PLC.instance.Tick();
      Assert.AreEqual(1, l1_v.input.layer);
      Assert.AreEqual(1, l1_v.output.layer);
      Assert.AreEqual(5, v2.As<int>());
      string json=l1_t.ToJson();
      Assert.AreEqual("{\"$type\":\"PiLink\",\"i\":\"/plc1/v1\",\"o\":\"/plc1/v2\"}", json);
    }
    [TestMethod]
    public void T21() {
      var p = Topic.root.Get("/plc2");
      var a1 = new PiBlock("ADD");
      var a1_t = p.Get("A01");
      var a1_a = a1_t.Get("A");
      var a1_q_t=a1_t.Get("Q");
      a1_t.value = a1;
      a1_a.value=3;
      PLC.instance.Tick();
      PLC.instance.Tick();
      Assert.AreEqual(1, a1._pins["A"].layer);
      Assert.AreEqual(2, a1.layer);
      Assert.AreEqual(2, a1._pins["Q"].layer);
      Assert.AreEqual(4, a1_q_t.As<int>());
      string json=a1_t.ToJson();
      Assert.AreEqual("{\"$type\":\"PiBlock\",\"func\":\"ADD\"}", json);
    }
    [TestMethod]
    public void T22() {
      var p = Topic.root.Get("/plc3");
      var a1_t = p.Get("A01");
      a1_t.SetJson("{\"$type\":\"PiBlock\",\"func\":\"ADD\"}");
      var a1_a = a1_t.Get("A");
      var a1_q_t=a1_t.Get("Q");
      var v1=p.Get("v1");
      var v2=p.Get("v2");
      var l1_t=p.Get("w001");
      l1_t.SetJson("{\"$type\":\"PiLink\",\"i\":\"/plc3/v1\",\"o\":\"/plc3/A01/A\"}");
      var l2_t=p.Get("w002");
      l2_t.SetJson("{\"$type\":\"PiLink\",\"i\":\"/plc3/A01/Q\",\"o\":\"/plc3/v2\"}");
      v1.value=28.3;
      PLC.instance.Tick();
      var a1=a1_t.As<PiBlock>();
      PLC.instance.Tick();
      Assert.AreEqual(1, a1._pins["A"].layer);
      Assert.AreEqual(2, a1.layer);
      Assert.AreEqual(2, a1._pins["Q"].layer);
      PLC.instance.Tick();
      //Assert.AreEqual(29.3, v2.As<double>());
    }

  }
}
