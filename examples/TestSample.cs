// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using NDesk.DBus;
using org.freedesktop.DBus;

public class ManagedDBusTestSample
{
	public static void Main ()
	{
		Bus bus = Bus.Session;

		SampleInterface sample = bus.GetObject<SampleInterface> ("com.example.SampleService", new ObjectPath ("/SomeObject"));

		Console.WriteLine ();
		string xmlData = sample.Introspect ();
		Console.WriteLine ("xmlData: " + xmlData);

		//object obj = sample.HelloWorld ("Hello from example-client.py!");
		string[] vals = sample.HelloWorld ("Hello from example-client.py!");
		foreach (string val in vals)
			Console.WriteLine (val);

		Console.WriteLine ();
		MyTuple tup = sample.GetTuple ();
		Console.WriteLine (tup.A);
		Console.WriteLine (tup.B);

		Console.WriteLine ();
		IDictionary<string,string> dict = sample.GetDict ();
		foreach (KeyValuePair<string,string> pair in dict)
			Console.WriteLine (pair.Key + ": " + pair.Value);
	}
}

[Interface ("com.example.SampleInterface")]
public interface SampleInterface : Introspectable
{
	//void HelloWorld (object hello_message);
	//object HelloWorld (object hello_message);
	string[] HelloWorld (object hello_message);
	MyTuple GetTuple ();
	IDictionary<string,string> GetDict ();
}

//(ss)
public struct MyTuple
{
	public string A;
	public string B;
}
