// Copyright 2007 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using DBus;
using org.freedesktop.DBus;

public class ManagedDBusTestExport
{
	public static void Main ()
	{
		Bus bus = Bus.Session;

		string bus_name = "org.ndesk.test";
		ObjectPath path = new ObjectPath ("/org/ndesk/test");

		IDemoOne demo;

		if (bus.RequestName (bus_name) == RequestNameReply.PrimaryOwner) {
			//create a new instance of the object to be exported
			demo = new Demo ();
			bus.Register (path, demo);

			//run the main loop
			while (true)
				bus.Iterate ();
		} else {
			//import a remote to a local proxy
			demo = bus.GetObject<IDemo> (bus_name, path);
			//demo = bus.GetObject<DemoProx> (bus_name, path);
		}

		Console.WriteLine ();
		demo.SomeEvent += HandleSomeEventA;
		demo.FireOffSomeEvent ();

		Console.WriteLine ();
		demo.SomeEvent -= HandleSomeEventA;
		demo.FireOffSomeEvent ();

		Console.WriteLine ();
		demo.SomeEvent += delegate (string arg1, object arg2, double arg3, MyTuple mt) {Console.WriteLine ("SomeEvent handler: " + arg1 + ", " + arg2 + ", " + arg3 + ", " + mt.A + ", " + mt.B);};
		demo.SomeEvent += delegate (string arg1, object arg2, double arg3, MyTuple mt) {Console.WriteLine ("SomeEvent handler two: " + arg1 + ", " + arg2 + ", " + arg3 + ", " + mt.A + ", " + mt.B);};
		demo.FireOffSomeEvent ();

		Console.WriteLine ();

		Console.WriteLine (demo.GetSomeVariant ());

		Console.WriteLine ();

		demo.Say2 ("demo.Say2");
		((IDemoTwo)demo).Say2 ("((IDemoTwo)demo).Say2");

		demo.SayEnum (DemoEnum.Bar, DemoEnum.Foo);

		uint n;
		string ostr;
		demo.WithOutParameters (out n, "21", out ostr);
		Console.WriteLine ("n: " + n);
		Console.WriteLine ("ostr: " + ostr);

		uint[] a1, a2, a3;
		demo.WithOutParameters2 (out a1, out a2, out a3);
		Console.WriteLine ("oparam2: " + a2[1]);

		uint[] @contacts = new uint[] { 2 };
		IDictionary<uint,SimplePresence> presence;
		demo.GetPresences (contacts, out presence);
		Console.WriteLine ("pres: " + presence[2].Status);

		MyTuple2 cpx = new MyTuple2 ();
		cpx.A = "a";
		cpx.B = "b";
		cpx.C = new Dictionary<int,MyTuple> ();
		cpx.C[3] = new MyTuple("foo", "bar");
		object cpxRet = demo.ComplexAsVariant (cpx, 12);
		MyTuple2 mt2ret = (MyTuple2)Convert.ChangeType (cpxRet, typeof (MyTuple2));
		Console.WriteLine ("mt2ret.C[3].B " + mt2ret.C[3].B);

		/*
		IDemoOne[] objs = demo.GetObjArr ();
		foreach (IDemoOne obj in objs)
			obj.Say ("Some obj");
		*/

		Console.WriteLine("SomeProp: " + demo.SomeProp);
		demo.SomeProp = 321;

		DemoProx demoProx = demo as DemoProx;
		if (demoProx != null)
			demoProx.SayRepeatedly(5, "Repetition");

		demo.ThrowSomeException ();
	}

	public static void HandleSomeEventA (string arg1, object arg2, double arg3, MyTuple mt)
	{
		Console.WriteLine ("SomeEvent handler A: " + arg1 + ", " + arg2 + ", " + arg3 + ", " + mt.A + ", " + mt.B);
	}

	public static void HandleSomeEventB (string arg1, object arg2, double arg3, MyTuple mt)
	{
		Console.WriteLine ("SomeEvent handler B: " + arg1 + ", " + arg2 + ", " + arg3 + ", " + mt.A + ", " + mt.B);
	}
}

[Interface ("org.ndesk.test")]
public interface IDemoOne
{
	event SomeEventHandler SomeEvent;
	void FireOffSomeEvent ();
	void Say (object var);
	void SayEnum (DemoEnum a, DemoEnum b);
	void Say2 (string str);
	object GetSomeVariant ();
	void ThrowSomeException ();
	void WithOutParameters (out uint n, string str, out string ostr);
	void WithOutParameters2 (out uint[] a1, out uint[] a2, out uint[] a3);
	void GetPresences (uint[] @contacts, out IDictionary<uint,SimplePresence> @presence);
	object ComplexAsVariant (object v, int num);

	IDemoOne[] GetEmptyObjArr ();
	IDemoOne[] GetObjArr ();
	int SomeProp { get; set; }
}

[Interface ("org.ndesk.test2")]
public interface IDemoTwo
{
	int Say (string str);
	void Say2 (string str);
}

public interface IDemo : IDemoOne, IDemoTwo
{
}

public abstract class DemoProx : DemoBase
{
	public virtual void SayRepeatedly (int count, string str)
	{
		for (int i = 0 ; i != count ; i++)
			Say2(str);
	}
}

public class Demo : DemoBase
{
	public override void Say2 (string str)
	{
		Console.WriteLine ("Subclassed IDemoOne.Say2: " + str);
	}
}

public class DemoBase : IDemo
{
	public event SomeEventHandler SomeEvent;

	public void Say (object var)
	{
		Console.WriteLine ("variant: " + var);
	}

	public int Say (string str)
	{
		Console.WriteLine ("string: " + str);
		return str.Length;
	}

	public void SayEnum (DemoEnum a, DemoEnum b)
	{
		Console.WriteLine ("SayEnum: " + a + ", " + b);
	}

	public virtual void Say2 (string str)
	{
		Console.WriteLine ("IDemoOne.Say2: " + str);
	}

	void IDemoTwo.Say2 (string str)
	{
		Console.WriteLine ("IDemoTwo.Say2: " + str);
	}

	public void FireOffSomeEvent ()
	{
		Console.WriteLine ("Asked to fire off SomeEvent");

		MyTuple mt;
		mt.A = "a";
		mt.B = "b";

		if (SomeEvent != null) {
			SomeEvent ("some string", 21, 19.84, mt);
			Console.WriteLine ("Fired off SomeEvent");
		}
	}

	public object GetSomeVariant ()
	{
		Console.WriteLine ("GetSomeVariant()");

		return new byte[0];
	}

	public void ThrowSomeException ()
	{
		throw new Exception ("Some exception");
	}

	public void WithOutParameters (out uint n, string str, out string ostr)
	{
		n = UInt32.Parse (str);
		ostr = "." + str + ".";
	}

	public void WithOutParameters2 (out uint[] a1, out uint[] a2, out uint[] a3)
	{
		a1 = new uint[] { };
		a2 = new uint[] { 21, 23, 16 };
		a3 = new uint[] { 21, 23 };
	}

	public void GetPresences (uint[] @contacts, out IDictionary<uint,SimplePresence> @presence)
	{
		Dictionary<uint,SimplePresence> presences = new Dictionary<uint,SimplePresence>();
		presences[2] = new SimplePresence { Type = ConnectionPresenceType.Offline, Status = "offline", StatusMessage = "" };
		presence = presences;
	}

	public object ComplexAsVariant (object v, int num)
	{
		Console.WriteLine ("v: " + v);
		Console.WriteLine ("v null? " + (v == null));

		MyTuple2 mt2 = (MyTuple2)Convert.ChangeType (v, typeof (MyTuple2));
		Console.WriteLine ("mt2.C[3].B " + mt2.C[3].B);
		Console.WriteLine ("num: " + num);

		return v;
	}

	public IDemoOne[] GetEmptyObjArr ()
	{
		return new Demo[] {};
	}

	public IDemoOne[] GetObjArr ()
	{
		return new IDemoOne[] {this};
	}

	public int SomeProp
	{
		get {
			return 123;
		} set {
			Console.WriteLine ("Set SomeProp: " + value);
		}
	}
}

public enum DemoEnum : byte
{
	Foo,
	Bar,
}


public struct MyTuple
{
	public MyTuple (string a, string b)
	{
		A = a;
		B = b;
	}

	public string A;
	public string B;
}

public struct MyTuple2
{
	public string A;
	public string B;
	public IDictionary<int,MyTuple> C;
}

public delegate void SomeEventHandler (string arg1, object arg2, double arg3, MyTuple mt);

public enum ConnectionPresenceType : uint
{
	Unset = 0, Offline = 1, Available = 2, Away = 3, ExtendedAway = 4, Hidden = 5, Busy = 6, Unknown = 7, Error = 8, 
}

public struct SimplePresence
{
	public ConnectionPresenceType Type;
	public string Status;
	public string StatusMessage;
}

