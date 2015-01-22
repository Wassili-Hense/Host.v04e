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
    private static string logFile;
    [ClassInitialize()]
    public static void MyClassInitialize(TestContext testContext) {
      root=Topic.root;
      logFile="PI_"+DateTime.Now.ToString("dd_HHmmss")+".log";
      X13.lib.Log.Write+=Log_Write;
    }

    static void Log_Write(X13.lib.LogLevel ll, DateTime dt, string msg) {
      System.IO.File.AppendAllText(logFile, string.Concat(dt.ToString("HH:mm:ss.ff"), " [", ll.ToString().Substring(0, 1), "] ", msg, "\r\n"));
    }

    [TestInitialize()]
    public void TestInitialize() {
      PLC.instance.Init();
      PLC.instance.Tick();
    }
    private void DefInc() {
      Topic.root.Get("/etc/PLC/func/NOT").SetJson("{\"$type\":\"PiDeclarer\",\"calc\":\"this.Q=this.A?false:true;\",\"pins\":{\"A\":{\"pos\":\"A\",\"mandatory\":true},\"Q\":{\"pos\":\"a\",\"mandatory\":true}}}");
      Topic.root.Get("/etc/PLC/func/INC").SetJson("{\"$type\":\"PiDeclarer\",\"calc\":\"this.Q=this.A+1;\",\"pins\":{\"A\":{\"pos\":\"A\",\"mandatory\":true},\"Q\":{\"pos\":\"a\",\"mandatory\":true}}}");
      Topic.root.Get("/etc/PLC/func/DEC").SetJson("{\"$type\":\"PiDeclarer\",\"calc\":\"this.Q=this.A-1;\",\"pins\":{\"A\":{\"pos\":\"A\",\"mandatory\":true},\"Q\":{\"pos\":\"a\",\"mandatory\":true}}}");
      Topic.root.Get("/etc/PLC/func/MOD").SetJson("{\"$type\":\"PiDeclarer\",\"calc\":\"this.Q=this.A%this.B;\",\"pins\":{\"A\":{\"pos\":\"A\",\"mandatory\":true},\"B\":{\"pos\":\"B\",\"mandatory\":true},\"Q\":{\"pos\":\"a\",\"mandatory\":true}}}");
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
      A1.Set(918);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(918, A1.As<long>());
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
      A1.Set(12);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(true, A1.As<bool>());
      A1.Set("false");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(true, A1.As<bool>());
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
      A1.Set("94");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(94, A1.As<long>());
      A1.Set("0x15");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(0x15, A1.As<long>());
      A1.Set("17.6");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(17, A1.As<long>());
      A1.Set(true);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(1, A1.As<long>());
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
      A1.Set("913");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(913.0, A1.As<double>());
      A1.Set("0x23");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(35.0, A1.As<double>());
      A1.Set("294.3187");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(294.3187, A1.As<double>());
      A1.Set(true);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(1.0, A1.As<double>());
      //A1.Set(DateTime.FromOADate(1638.324));
      //X13.PLC.PLC.instance.Tick();
      //Assert.AreEqual(1638.324, A1.As<double>());
    }
    [TestMethod]
    public void T07() {
      Topic A3=root.Get("A3");
      A3.Set(1022);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(1022, A3.As<long>());
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
      PLC.instance.Tick();
      arr=t0.children.ToArray();
      Assert.AreEqual(1, arr.Length);
      Assert.AreEqual(t1, arr[0]);
      t1=t0.Get("ch_b");
      var t2=t1.Get("a");
      t2=t1.Get("b");
      t1=t0.Get("ch_c");
      t2=t1.Get("a");
      PLC.instance.Tick();
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
      Assert.AreEqual(b3, cmds1[1].src);
      Assert.AreEqual(Perform.Art.move, cmds1[1].art);
      Assert.AreEqual(c3, cmds1[0].src);
      Assert.AreEqual(Perform.Art.create, cmds1[0].art);
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
      Assert.AreEqual(c3, cmds1[2].src);
      Assert.AreEqual(Perform.Art.move, cmds1[2].art);
      Assert.AreEqual(d3, cmds1[0].src);
      Assert.AreEqual(Perform.Art.create, cmds1[0].art);
      Assert.AreEqual(c3_a, cmds1[1].src);
      Assert.AreEqual(Perform.Art.create, cmds1[1].art);
      cmds1.Clear();

      d3.Set(17);
      var e3=d3.Move(root, "e3");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(17, e3.As<long>());
      cmds1.Clear();
    }
    [TestMethod]
    public void T19() {
      var t1=Topic.root.Get("/A/A_A/A_A_A");
      var t2=t1.parent.Get("A_A_B/A_A_B_A");
      string rp=PiLink.RelativePath(t1, t2);
      Assert.AreEqual("../A_A_B/A_A_B_A", rp);
      rp=PiLink.RelativePath(t2, t1);
      Assert.AreEqual("../../A_A_A", rp);
      var t3=t2.Get("A_A_B_A_A");
      rp=PiLink.RelativePath(t1, t3);
      Assert.AreEqual(t3.path, rp);
      rp=PiLink.RelativePath(t3, t1);
      Assert.AreEqual(t1.path, rp);
    }
    /// <summary>Link (var, var)</summary>
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
      Assert.AreEqual(2, l1_v.output.layer);
      Assert.AreEqual(5, v2.As<int>());

      string json = l1_t.ToJson();
      Assert.AreEqual("{\"$type\":\"PiLink\",\"i\":\"v1\",\"o\":\"v2\"}", json);
    }
    /// <summary>Block(INC)</summary>
    [TestMethod]
    public void T21() {
      DefInc();
      var p = Topic.root.Get("/plc2");
      var a1 = new PiBlock("INC");
      var a1_t = p.Get("A01");
      var a1_a = a1_t.Get("A");
      var a1_q_t=a1_t.Get("Q");
      a1_t.value = a1;
      a1_a.value=3;
      PLC.instance.Tick();
      Assert.AreEqual(1, a1._pins["A"].layer);
      Assert.AreEqual(2, a1.layer);
      Assert.AreEqual(2, a1._pins["Q"].layer);
      Assert.AreEqual(4, a1_q_t.As<int>());
      string json=a1_t.ToJson();
      Assert.AreEqual("{\"$type\":\"PiBlock\",\"func\":\"INC\"}", json);
    }
    /// <summary>Link (var, alias)</summary>
    [TestMethod]
    public void T22() {
      var p=Topic.root.Get("/plc22");
      var v1=p.Get("v1");
      var v2=p.Get("v2");
      var k2_t=p.Get("v2_alias");
      k2_t.value=new PiAlias(v2);
      var l1_v=new PiLink(v1, k2_t);
      var l1_t=p.Get("w001");
      l1_t.value=l1_v;
      v1.Set(42);
      PLC.instance.Tick();
      Assert.AreEqual(1, l1_v.input.layer);
      Assert.AreEqual(2, l1_v.output.layer);
      Assert.AreEqual(42, v2.As<int>());

      v1.Set(43);
      PLC.instance.Tick();
      Assert.AreEqual(43, v2.As<int>());

      string json = k2_t.ToJson();
      Assert.AreEqual("{\"$type\":\"PiAlias\",\"alias\":\"/plc22/v2\"}", json);
      json = l1_t.ToJson();
      Assert.AreEqual("{\"$type\":\"PiLink\",\"i\":\"v1\",\"o\":\"v2_alias\"}", json);
    }
    /// <summary>Link (alias, var)</summary>
    [TestMethod]
    public void T23() {
      var p=Topic.root.Get("/plc23");
      var v1=p.Get("v1");
      var v2=p.Get("v2");
      var k1_t=p.Get("v1_alias");
      k1_t.value=new PiAlias(v1);
      var l1_v=new PiLink(k1_t, v2);
      var l1_t=p.Get("w001");
      l1_t.value=l1_v;
      v1.Set(24);
      PLC.instance.Tick();
      Assert.AreEqual(1, l1_v.input.layer);
      Assert.AreEqual(2, l1_v.output.layer);
      Assert.AreEqual(24, v2.As<int>());

      v1.Set(23);
      PLC.instance.Tick();
      Assert.AreEqual(23, v2.As<int>());

      string json=k1_t.ToJson();
      Assert.AreEqual("{\"$type\":\"PiAlias\",\"alias\":\"/plc23/v1\"}", json);
      json=l1_t.ToJson();
      Assert.AreEqual("{\"$type\":\"PiLink\",\"i\":\"v1_alias\",\"o\":\"v2\"}", json);
    }
    /// <summary>Block(INC, A=alias, Q=alias), Block(MOD, A=A01/Q, B=7, Q=>alias)</summary>
    [TestMethod]
    public void T24() {
      DefInc();
      var p = Topic.root.Get("/plc24");

      var v1=p.Get("v1");
      var k1_t=p.Get("v1_alias");
      k1_t.value=new PiAlias(v1);

      var a01 = new PiBlock("INC");
      var a01_t = p.Get("A01");
      a01_t.value = a01;

      p.Get("w001").value=new PiLink(k1_t, a01_t.Get("A"));

      var v2=p.Get("v2");
      var k2_t=p.Get("v2_alias");
      k2_t.value=new PiAlias(v2);

      p.Get("w002").value=new PiLink(a01_t.Get("Q"), k2_t);

      v1.value=28.3;
      PLC.instance.Tick();
      Assert.AreEqual(2, a01._pins["A"].layer);
      Assert.AreEqual(3, a01.layer);
      Assert.AreEqual(3, a01._pins["Q"].layer);
      Assert.AreEqual(29.3, v2.As<double>());

      PLC.Export("T24.xst", Topic.root);

      v1.value=1023;
      PLC.instance.Tick();
      Assert.AreEqual(1024, v2.As<int>());

      var a02 = new PiBlock("MOD");
      var a02_t = p.Get("A02");
      var a02_b_t=a02_t.Get("B");
      var a02_q_t=a02_t.Get("Q");
      a02_t.value = a02;

      var v3=p.Get("v3");
      var k3_t=p.Get("v3_alias");
      k3_t.value=new PiAlias(v3);

      p.Get("w003").value=new PiLink(a01_t.Get("Q"), a02_t.Get("A"));
      p.Get("w004").value=new PiLink(a02_t.Get("Q"), k3_t);

      v1.value=19;
      a02_b_t.value=7;

      PLC.instance.Tick();

      Assert.AreEqual(4, a02._pins["A"].layer);
      Assert.AreEqual(5, a02.layer);
      Assert.AreEqual(5, a02._pins["Q"].layer);
      Assert.AreEqual(6, v3.As<int>());

      PLC.Export("T24.xst", Topic.root);
    }
    [TestMethod]
    public void T25() {
      PLC.Import("T25.xst");
      PLC.instance.Tick();
      var p = Topic.root.Get("/plc25");
      var v1=p.Get("v1");
      var v3=p.Get("v3");
      int i=499;
      //var w=new System.Diagnostics.Stopwatch();
      //w.Start();
      //for(i=0; i<500; i++) 
      {
        v1.value=i;
        PLC.instance.Tick();
        Assert.AreEqual((i+1)%7, v3.As<int>());
      }
      //w.Stop();
      //X13.lib.Log.Info("T25 time={0}", w.Elapsed.TotalMilliseconds);

      var a03 = new PiBlock("DEC");
      var a03_t = p.Get("A03");
      a03_t.value = a03;

      var k3_t=p.Get("v3_alias");

      p.Get("w004").value=new PiLink(a03_t.Get("Q"), k3_t);
      p.Get("w005").value=new PiLink(p.Get("A02/Q"), a03_t.Get("A"));

      PLC.instance.Tick();
      Assert.AreEqual(((i+1)%7)-1, v3.As<int>());

      v1.value=12;
      PLC.instance.Tick();
      Assert.AreEqual(5, v3.As<int>());
    }
    /// <summary>Block(NOT, A, Q), Link(A, Q)</summary>
    [TestMethod]
    public void T26() {
      DefInc();
      var p = Topic.root.Get("/plc26");
      var a01 = new PiBlock("NOT");
      var a01_t = p.Get("A01");
      var a01_q_t=a01_t.Get("Q");
      a01_t.value = a01;
      p.Get("w001").value=new PiLink(a01_q_t, a01_t.Get("A"));
      PLC.instance.Tick();
      Assert.IsNotNull(a01._decl, "func/NOT not found");
      Assert.AreEqual(true, a01_q_t.As<bool>());

      PLC.instance.Tick();
      Assert.AreEqual(false, a01_q_t.As<bool>());

      PLC.instance.Tick();
      Assert.AreEqual(true, a01_q_t.As<bool>());

      PLC.instance.Tick();
      Assert.AreEqual(false, a01_q_t.As<bool>());

      PLC.instance.Tick();
      Assert.AreEqual(true, a01_q_t.As<bool>());
    }
  }
}
