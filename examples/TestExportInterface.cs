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

		ObjectPath myOpath = new ObjectPath ("/org/ndesk/test");
		string myNameReq = "org.ndesk.test";

		IDemoObject demo;

		if (bus.NameHasOwner (myNameReq)) {
			demo = bus.GetObject<IDemo> (myNameReq, myOpath);
		} else {
			demo = new DemoObject ();
			bus.Register (myNameReq, myOpath, demo);

			RequestNameReply nameReply = bus.RequestName (myNameReq);
			Console.WriteLine ("RequestNameReply: " + nameReply);

			while (true)
				bus.Iterate ();
		}

		Console.WriteLine ();
		demo.SomeEvent += delegate (string arg1, object arg2, double arg3, MyTuple mt) {Console.WriteLine ("SomeEvent handler: " + arg1 + ", " + arg2 + ", " + arg3 + ", " + mt.A + ", " + mt.B);};
		demo.SomeEvent += delegate (string arg1, object arg2, double arg3, MyTuple mt) {Console.WriteLine ("SomeEvent handler two: " + arg1 + ", " + arg2 + ", " + arg3 + ", " + mt.A + ", " + mt.B);};
		demo.FireOffSomeEvent ();
		//handle the raised signal
		//bus.Iterate ();

		Console.WriteLine ();
		demo.SomeEvent += HandleSomeEventA;
		demo.FireOffSomeEvent ();
		//handle the raised signal
		//bus.Iterate ();

		Console.WriteLine ();
		demo.SomeEvent -= HandleSomeEventA;
		demo.FireOffSomeEvent ();
		//handle the raised signal
		//bus.Iterate ();

		Console.WriteLine ();

		Console.WriteLine (demo.GetSomeVariant ());

		Console.WriteLine ();

		demo.Say2 ("demo.Say2");
		((IDemoObjectTwo)demo).Say2 ("((IDemoObjectTwo)demo).Say2");

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
public interface IDemoObject
{
	event SomeEventHandler SomeEvent;
	void FireOffSomeEvent ();
	void Say (object var);
	void Say2 (string str);
	object GetSomeVariant ();
	void ThrowSomeException ();
}

[Interface ("org.ndesk.test2")]
public interface IDemoObjectTwo
{
	int Say (string str);
	void Say2 (string str);
}

public interface IDemo : IDemoObject, IDemoObjectTwo
{
}

public class DemoObject : IDemo
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

	public void Say2 (string str)
	{
		Console.WriteLine ("IDemoObject.Say2: " + str);
	}

	void IDemoObjectTwo.Say2 (string str)
	{
		Console.WriteLine ("IDemoObjectTwo.Say2: " + str);
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

		return new byte[] {3, 2, 1};
	}

	public void ThrowSomeException ()
	{
		throw new Exception ("Some exception");
	}
}

public enum DemoEnum
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
