// Copyright 2007 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using NDesk.DBus;
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

		/*
		uint n;
		string ostr;
		demo.WithOutParameters (out n, "21", out ostr);
		Console.WriteLine ("n: " + n);
		Console.WriteLine ("ostr: " + ostr);
		*/

		/*
		IDemoOne[] objs = demo.GetObjArr ();
		foreach (IDemoOne obj in objs)
			obj.Say ("Some obj");
		*/

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
	IDemoOne[] GetEmptyObjArr ();
	IDemoOne[] GetObjArr ();
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
		Console.WriteLine ("IDemoOne.Say2: " + a + ", " + b);
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

	public IDemoOne[] GetEmptyObjArr ()
	{
		return new Demo[] {};
	}

	public IDemoOne[] GetObjArr ()
	{
		return new IDemoOne[] {this};
	}
}

public enum DemoEnum : byte
{
	Foo,
	Bar,
}


public struct MyTuple
{
	public string A;
	public string B;
}

public delegate void SomeEventHandler (string arg1, object arg2, double arg3, MyTuple mt);
