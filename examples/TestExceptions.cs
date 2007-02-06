// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using NDesk.DBus;
using org.freedesktop.DBus;

public class ManagedDBusTestExceptions
{
	public static void Main ()
	{
		Bus bus = Bus.Session;

		string myNameReq = "org.ndesk.testexceptions";
		ObjectPath myOpath = new ObjectPath ("/org/ndesk/testexceptions");

		DemoObject demo;

		if (bus.NameHasOwner (myNameReq)) {
			demo = bus.GetObject<DemoObject> (myNameReq, myOpath);
		} else {
			demo = new DemoObject ();
			bus.Register (myNameReq, myOpath, demo);

			RequestNameReply nameReply = bus.RequestName (myNameReq, NameFlag.None);
			Console.WriteLine ("RequestNameReply: " + nameReply);

			while (true)
				bus.Iterate ();
		}

		Console.WriteLine ();
		//org.freedesktop.DBus.Error.InvalidArgs: Requested bus name "" is not valid
		try {
			bus.RequestName ("");
		} catch (Exception e) {
			Console.WriteLine (e);
		}

		//TODO: make this work as expected (what is expected?)
		Console.WriteLine ();
		try {
			demo.ThrowSomeException ();
		} catch (Exception e) {
			Console.WriteLine (e);
		}

		Console.WriteLine ();
		try {
			demo.ThrowSomeExceptionNoRet ();
		} catch (Exception e) {
			Console.WriteLine (e);
		}
		//handle the thrown exception
		//conn.Iterate ();

		Console.WriteLine ();
		try {
			demo.HandleVariant (null);
		} catch (Exception e) {
			Console.WriteLine (e);
		}

		Console.WriteLine ();
		try {
			demo.HandleString (null);
		} catch (Exception e) {
			Console.WriteLine (e);
		}

		Console.WriteLine ();
		try {
			demo.HandleArray (null);
		} catch (Exception e) {
			Console.WriteLine (e);
		}
	}
}

[Interface ("org.ndesk.testexceptions")]
public class DemoObject : MarshalByRefObject
{
	public int ThrowSomeException ()
	{
		Console.WriteLine ("Asked to throw some Exception");

		throw new Exception ("Some Exception");
	}

	public void ThrowSomeExceptionNoRet ()
	{
		Console.WriteLine ("Asked to throw some Exception NoRet");

		throw new Exception ("Some Exception NoRet");
	}

	public void HandleVariant (object o)
	{
		Console.WriteLine (o);
	}

	public void HandleString (string str)
	{
		Console.WriteLine (str);
	}

	public void HandleArray (byte[] arr)
	{
		Console.WriteLine (arr);
	}
}
