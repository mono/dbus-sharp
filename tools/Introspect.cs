// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using NDesk.DBus;
using Schemas;

public class test
{
	public static void Main (string[] args)
	{
		string fname = args[0];
		StreamReader sr = new StreamReader (fname);
		XmlSerializer sz = new XmlSerializer (typeof (Node));
		Node node = (Node)sz.Deserialize (sr);

		Interface iface = node.Interfaces[1];

		foreach (Method meth in iface.Methods) {
			Console.Write (meth.Name);
			Console.Write (" (");

			if (meth.Arguments != null)
			foreach (Argument arg in meth.Arguments)
				Console.Write ("[" + arg.Direction + "] " + arg.Type + " " + arg.Name + ", ");
			Console.Write (");");
			Console.WriteLine ();
		}
	}
}
