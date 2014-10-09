using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using X13.PLC;

namespace X13 {
  /// <summary>
  /// Zusammenfassungsbeschreibung für Topic_UT
  /// </summary>
  [TestClass]
  public class Topic_UT {
    public Topic_UT() {
      //
      // TODO: Konstruktorlogik hier hinzufügen
      //
    }

    private TestContext testContextInstance;

    /// <summary>
    ///Ruft den Textkontext mit Informationen über
    ///den aktuellen Testlauf sowie Funktionalität für diesen auf oder legt diese fest.
    ///</summary>
    public TestContext TestContext {
      get {
        return testContextInstance;
      }
      set {
        testContextInstance = value;
      }
    }

    #region Zusätzliche Testattribute
    //
    // Sie können beim Schreiben der Tests folgende zusätzliche Attribute verwenden:
    //
    // Verwenden Sie ClassInitialize, um vor Ausführung des ersten Tests in der Klasse Code auszuführen.
    // [ClassInitialize()]
    // public static void MyClassInitialize(TestContext testContext) { }
    //
    // Verwenden Sie ClassCleanup, um nach Ausführung aller Tests in einer Klasse Code auszuführen.
    // [ClassCleanup()]
    // public static void MyClassCleanup() { }
    //
    // Mit TestInitialize können Sie vor jedem einzelnen Test Code ausführen. 
    // [TestInitialize()]
    // public void MyTestInitialize() { }
    //
    // Mit TestCleanup können Sie nach jedem einzelnen Test Code ausführen.
    // [TestCleanup()]
    // public void MyTestCleanup() { }
    //
    #endregion

    private static Topic root;
    private static Random r;
    [ClassInitialize()]
    public static void MyClassInitialize(TestContext testContext) {
      r=new Random((int)DateTime.Now.Ticks);
      root=Topic.root;
      //root.SetJson("{\"$type\":\"X13.Engine_UT.TestObj, Engine_UT\",\"A\":315,\"B\":0.41}");
      //X13.PLC.PLC.instance.Tick();
      //root.ToJson();
    }
    [TestInitialize()]
    public void TestInitialize() {
      X13.PLC.PLC.instance.Clear();
      X13.PLC.PLC.instance.Tick();
    }

    private List<Perform> cmds1;
    private void cmds1Fire(Topic t, Perform c) {
      cmds1.Add(c);
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
    public void T03() {
      Topic A1=root.Get("A1");
      long val=r.Next();
      A1.Set(val);
      Topic A2=root.Get("/A2");
      A2.Set(A1);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(val, A2.As<long>());

      val=r.Next();
      A1.Set(val);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(val, A2.As<long>());

      var now=DateTime.Now;
      A2.Set(now);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(now, A1.As<DateTime>());

      A2.Set<Topic>(null);   // reset reference
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(null, A2.As<object>());

      A2.Set(true);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(now, A1.As<DateTime>());
      Assert.AreEqual(true, A2.As<object>());
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
      A1.Set("false");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(false, A1.As<bool>());
      A1.Set("True");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(true, A1.As<bool>());
    }
    [TestMethod]
    public void T05() {   // parse to long
      Topic A1=root.Get("A1");
      A1.Set((object)257);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(257, A1.As<long>());
      A1.Set(25.7);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(25, A1.As<long>());
      A1.Set("94");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(94, A1.As<long>());
      A1.Set("0x15");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(0, A1.As<long>());
      A1.Set("17.6");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(17, A1.As<long>());
      A1.Set(true);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(1, A1.As<long>());
      A1.Set(new DateTime(917L));
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(917, A1.As<long>());
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
      A1.Set("0x15");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(0.0, A1.As<double>());
      A1.Set("294.3187");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(294.3187, A1.As<double>());
      A1.Set(true);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(1.0, A1.As<double>());
      A1.Set(DateTime.FromOADate(1638.324));
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(1638.324, A1.As<double>());
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
      Topic t0=root.Get("child1");
      cmds1=new List<Perform>();
      X13.PLC.PLC.instance.Tick();
      t0.changed+=cmds1Fire;
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
      t0.changed-=cmds1Fire;
      t0.Set(2.98);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(0, cmds1.Count);
      cmds1.Clear();
    }
    [TestMethod]
    public void T10() {
      Topic A1=root.Get("A1");
      long val=r.Next();
      A1.Set(val);
      Topic A2=root.Get("/A2");
      cmds1=new List<Perform>();
      A2.changed+=cmds1Fire;
      A2.Set(A1);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(val, A2.As<long>());
      Assert.AreEqual(2, cmds1.Count);
      Assert.AreEqual(A2, cmds1[0].src);
      Assert.AreEqual(Perform.Art.create, cmds1[0].art);
      Assert.AreEqual(A2, cmds1[1].src);
      Assert.AreEqual(Perform.Art.changed, cmds1[1].art);
      cmds1.Clear();

      val=r.Next();
      A1.Set(val);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(val, A2.As<long>());
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(A1, cmds1[0].src);
      cmds1.Clear();

      var now=DateTime.Now;
      A2.Set(now);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(now, A1.As<DateTime>());
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(A1, cmds1[0].src);
      cmds1.Clear();

      A2.Set<Topic>(null);   // reset reference
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(null, A2.As<object>());
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(A2, cmds1[0].src);
      cmds1.Clear();

      A2.Set(true);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(now, A1.As<DateTime>());
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(A2, cmds1[0].src);
      cmds1.Clear();

      val=r.Next();
      A1.Set(val);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(val, A1.As<long>());
      Assert.AreEqual(0, cmds1.Count);
      cmds1.Clear();
    }
    [TestMethod]
    public void T11() {
      Topic t0=root.Get("child2");
      var t1=t0.Get("ch_a");
      var t1_a=t1.Get("a");
      cmds1=new List<Perform>();
      X13.PLC.PLC.instance.Tick();
      t0.children.changed+=cmds1Fire;
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
      cmds1.Clear();
    }
    [TestMethod]
    public void T12() {
      Topic t0=root.Get("child3");
      var t1=t0.Get("ch_a");
      var t1_a=t1.Get("a");
      cmds1=new List<Perform>();
      X13.PLC.PLC.instance.Tick();
      t0.all.changed+=cmds1Fire;
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
      cmds1=new List<Perform>();

      var b3=root.Get("B3");
      X13.PLC.PLC.instance.Tick();
      b3.all.changed+=cmds1Fire;
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
    public void T15() {
      cmds1=new List<Perform>();

      var r=Topic.root.Get("t15");
      var a1=r.Get("a1");
      var a2=r.Get("a2");
      a1.Set(13);
      a2.Set(a1);
      a2.changed+=cmds1Fire;
      X13.PLC.PLC.instance.Tick();
      cmds1.Clear();

      Assert.AreEqual(13, a2.As<long>());
      var b1=a1.Move(r, "b1");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(b1, a2.As<Topic>());
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(a2, cmds1[0].src);
      Assert.AreEqual(Perform.Art.changed, cmds1[0].art);
      cmds1.Clear();

      b1.Set(15);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(15, a2.As<long>());
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(b1, cmds1[0].src);
      Assert.AreEqual(Perform.Art.changed, cmds1[0].art);
      cmds1.Clear();

      b1.Remove();
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(null, a2.As<Topic>());
      Assert.AreEqual(15, a2.As<long>());
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(a2, cmds1[0].src);
      Assert.AreEqual(Perform.Art.changed, cmds1[0].art);
      cmds1.Clear();

    }

    [TestMethod]
    public void T16() {
      Topic t16=Topic.root.Get("/t16");
      X13.PLC.PLC.instance.Tick();

      t16.SetJson("true");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(typeof(bool), t16.vType);
      Assert.AreEqual(true, t16.As<bool>());

      t16.SetJson("137");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(typeof(long), t16.vType);
      Assert.AreEqual(137, t16.As<long>());

      t16.SetJson("35.97");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(typeof(double), t16.vType);
      Assert.AreEqual(35.97, t16.As<double>());
      
      t16.SetJson("8.945E7");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(typeof(double), t16.vType);
      Assert.AreEqual(8.945e7, t16.As<double>());

      t16.SetJson("'2014-04-15T01:23:45.000Z'");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(typeof(DateTime), t16.vType);
      Assert.AreEqual(new DateTime(2014, 04, 15, 01, 23, 45), t16.As<DateTime>());

      t16.SetJson("\"Hello\"");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual(typeof(string), t16.vType);
      Assert.AreEqual("Hello", t16.As<string>());

      var a=t16.Get("a");
      a.SetJson("{\"$ref\":\"/t16/b\"}");
      X13.PLC.PLC.instance.Tick();
      Topic b;
      Assert.AreEqual(true, t16.Exist("b", out b));
      Assert.AreEqual(typeof(Topic), a.vType);
      Assert.AreEqual(b, a.As<Topic>());
    }

    [TestMethod]
    public void T17() {
      Topic r=Topic.root.Get("/t17");
      r.Set(false);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual("false", r.ToJson());

      r.Set(34);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual("34", r.ToJson());

      r.Set(28.0);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual("28.0", r.ToJson());

      r.Set(28.09);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual("28.09", r.ToJson());

      r.Set(3.195e-6);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual("3.195E-6", r.ToJson());

      r.Set(new DateTime(2014, 05, 06, 23, 45, 56));
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual("'2014-05-06T21:45:56.000Z'", r.ToJson());

      r.Set("X13.HomeAutomation");
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual("\"X13.HomeAutomation\"", r.ToJson());

      var a=r.Get("a");
      var b=r.Get("b");
      a.Set(b);
      X13.PLC.PLC.instance.Tick();
      Assert.AreEqual("{\"$ref\":\"/t17/b\"}", a.ToJson());
    }
    [TestMethod]
    public void U01() {
      Assert.AreEqual(6.0, X13.PLC.PLC.instance.Engine.Evaluate("x = 1 + \r\n 5"));
    }

    //[TestMethod] public void T01() { }
    //[TestMethod] public void T01() { }
    //[TestMethod] public void T01() { }
    //[TestMethod] public void T01() { }
    //[TestMethod] public void T01() { }
  }
}
