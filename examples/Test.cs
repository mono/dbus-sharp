// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NDesk.DBus;
using org.freedesktop.DBus;

public class ManagedDBusTest
{
	public static void Main ()
	{
		Connection conn = Connection.Open (Address.Session);

		ObjectPath opath = new ObjectPath ("/org/freedesktop/DBus");
		string name = "org.freedesktop.DBus";

		IBus bus = conn.GetObject<IBus> (name, opath);

		bus.NameAcquired += delegate (string acquired_name) {
			Console.WriteLine ("NameAcquired: " + acquired_name);
		};

		string myName = bus.Hello ();
		Console.WriteLine ("myName: " + myName);

		Console.WriteLine ();
		string xmlData = bus.Introspect ();
		Console.WriteLine ("xmlData: " + xmlData);

		Console.WriteLine ();
		foreach (string n in bus.ListNames ())
			Console.WriteLine (n);

		Console.WriteLine ();
		foreach (string n in bus.ListNames ())
			Console.WriteLine ("Name " + n + " has owner: " + bus.NameHasOwner (n));

		Console.WriteLine ();
		//Console.WriteLine ("NameHasOwner: " + dbus.NameHasOwner (name));
		//Console.WriteLine ("NameHasOwner: " + dbus.NameHasOwner ("fiz"));
	}
}
