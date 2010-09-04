// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using DBus;
using org.freedesktop.DBus;

class BusMonitor
{
	static Connection bus = null;

	public static int Main (string[] args)
	{
		bus = null;
		bool readIn = false;
		bool readOut = false;
		List<string> rules = new List<string> ();

		for (int i = 0 ; i != args.Length ; i++) {
			string arg = args[i];

			if (!arg.StartsWith ("--")) {
				rules.Add (arg);
				continue;
			}

			switch (arg)
			{
				case "--stdin":
					readIn = true;
					break;
				case "--stdout":
					readOut = true;
					break;
				case "--system":
					bus = Bus.System;
					break;
				case "--session":
					bus = Bus.Session;
					break;
				default:
					Console.Error.WriteLine ("Usage: monitor.exe [--stdin|--stdout|--system|--session] [watch expressions]");
					Console.Error.WriteLine ("       If no watch expressions are provided, defaults will be used.");
					return 1;
			}
		}

		if (bus == null)
			bus = Bus.Session;

		if (rules.Count != 0) {
			//install custom match rules only
			foreach (string rule in rules)
				bus.AddMatch (rule);
		} else {
			//no custom match rules, install the defaults
			bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.Signal));
			bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.MethodReturn));
			bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.Error));
			bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.MethodCall));
		}

		if (readIn) {
			ReadIn ();
			return 0;
		}

		if (readOut) {
			ReadOut ();
			return 0;
		}

		PrettyPrintOut ();
		return 0;
	}

	static void ReadIn ()
	{
		TextReader r = Console.In;

		while (true) {
			Message msg = MessageDumper.ReadMessage (r);
			if (msg == null)
				break;
			PrintMessage (msg);
			Console.WriteLine ();

			/*
			byte[] header = MessageDumper.ReadBlock (r);
			if (header == null)
				break;
			PrintHeader (header);

			byte[] body = MessageDumper.ReadBlock (r);
			PrintBody (header);
			*/
		}
	}

	static void ReadOut ()
	{
		TextWriter w = Console.Out;

		DumpConn (bus, w);

		while (true) {
			Message msg = bus.Transport.ReadMessage ();
			if (msg == null)
				break;
			DumpMessage (msg, w);
		}
	}

	static void PrettyPrintOut ()
	{
		while (true) {
			Message msg = bus.Transport.ReadMessage ();
			if (msg == null)
				break;
			PrintMessage (msg);
			Console.WriteLine ();
		}
	}

	static void DumpConn (Connection conn, TextWriter w)
	{
		w.WriteLine ("# This is a managed D-Bus protocol dump");
		w.WriteLine ();
		w.WriteLine ("# Machine: " + Connection.MachineId);
		w.WriteLine ("# Connection: " + conn.Id);
		w.WriteLine ("# Date: " + DateTime.Now.ToString ("F"));
		w.WriteLine ();
	}

	static DateTime startTime = DateTime.Now;
	static void DumpMessage (Message msg, TextWriter w)
	{
		w.WriteLine ("# Message: " + msg.Header.Serial);

		TimeSpan delta = DateTime.Now - startTime;
		startTime = DateTime.Now;
		w.WriteLine ("# Time delta: " + delta.Ticks);

		w.WriteLine ("# Header");
		MessageDumper.WriteBlock (msg.GetHeaderData (), w);
		w.WriteLine ("# Body");
		MessageDumper.WriteBlock (msg.Body, w);

		w.WriteLine ();
		w.Flush ();
	}

	const string indent = "  ";

	static void PrintMessage (Message msg)
	{
		Console.WriteLine ("Message (" + msg.Header.Endianness + " endian, v" + msg.Header.MajorVersion + "):");
		Console.WriteLine (indent + "Type: " + msg.Header.MessageType);
		Console.WriteLine (indent + "Flags: " + msg.Header.Flags);
		Console.WriteLine (indent + "Serial: " + msg.Header.Serial);

		//foreach (HeaderField hf in msg.HeaderFields)
		//	Console.WriteLine (indent + hf.Code + ": " + hf.Value);
		Console.WriteLine (indent + "Header Fields:");
		foreach (KeyValuePair<byte,object> field in msg.Header.Fields)
			Console.WriteLine (indent + indent + field.Key + ": " + field.Value);

		Console.WriteLine (indent + "Body (" + msg.Header.Length + " bytes):");
		if (msg.Body != null) {
			MessageReader reader = new MessageReader (msg);

			int argNum = 0;
			foreach (Signature sig in msg.Signature.GetParts ()) {
				//Console.Write (indent + indent + "arg" + argNum + " " + sig + ": ");
				PrintValue (reader, sig, 1);
				/*
				if (sig.IsPrimitive) {
					object arg = reader.ReadValue (sig[0]);
					Console.WriteLine (arg);
				} else {
					if (sig.IsArray) {
						//foreach (Signature elemSig in writer.StepInto (sig))
					}
					reader.StepOver (sig);
					Console.WriteLine ("?");
				}
				*/
				argNum++;
			}
		}
	}

	static void PrintValue (MessageReader reader, Signature sig, int depth)
	{
		string indent = new String (' ', depth * 2);
		indent += "  ";

		//Console.Write (indent + indent + "arg" + argNum + " " + sig + ": ");
		Console.Write (indent);
		if (sig == Signature.VariantSig) {
			foreach (Signature elemSig in reader.StepInto (sig)) {
				Console.WriteLine ("Variant '{0}' (", elemSig);
				PrintValue (reader, elemSig, depth + 1);
				Console.WriteLine (indent + ")");
			}
		} else if (sig.IsPrimitive) {
			object arg = reader.ReadValue (sig[0]);
			Type argType = sig.ToType ();
			if (sig == Signature.StringSig || sig == Signature.ObjectPathSig)
				Console.WriteLine ("{0} \"{1}\"", argType.Name, arg);
			else if (sig == Signature.SignatureSig)
				Console.WriteLine ("{0} '{1}'", argType.Name, arg);
			else
				Console.WriteLine ("{0} {1}", argType.Name, arg);
		} else if (sig.IsArray) {
			Console.WriteLine ("Array [");
			foreach (Signature elemSig in reader.StepInto (sig))
				PrintValue (reader, elemSig, depth + 1);
			Console.WriteLine (indent + "]");
		} else if (sig.IsDictEntry) {
			Console.WriteLine ("DictEntry {");
			foreach (Signature elemSig in reader.StepInto (sig))
				PrintValue (reader, elemSig, depth + 1);
			Console.WriteLine (indent + "}");
		} else if (sig.IsStruct) {
			Console.WriteLine ("Struct {");
			foreach (Signature elemSig in reader.StepInto (sig))
				PrintValue (reader, elemSig, depth + 1);
			Console.WriteLine (indent + "}");
		} else {
			reader.StepOver (sig);
			Console.WriteLine ("'{0}'?", sig);
		}
	}
}
