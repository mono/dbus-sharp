// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using NDesk.DBus;
using org.freedesktop.DBus;

public class Monitor
{
	public static void Main (string[] args)
	{
		Connection bus;

		if (args.Length >= 1) {
			string arg = args[0];

			switch (arg)
			{
				case "--system":
					bus = Bus.System;
					break;
				case "--session":
					bus = Bus.Session;
					break;
				default:
					Console.Error.WriteLine ("Usage: monitor.exe [--system | --session] [watch expressions]");
					Console.Error.WriteLine ("       If no watch expressions are provided, defaults will be used.");
					return;
			}
		} else {
			bus = Bus.Session;
		}

		if (args.Length > 1) {
			//install custom match rules only
			for (int i = 1 ; i != args.Length ; i++)
				bus.AddMatch (args[i]);
		} else {
			//no custom match rules, install the defaults
			bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.Signal));
			bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.MethodReturn));
			bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.Error));
			bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.MethodCall));
		}

		while (true) {
			Message msg = bus.ReadMessage ();
			if (msg == null)
				break;
			PrintMessage (msg);
			Console.WriteLine ();
		}
	}

	const string indent = "  ";

	internal static void PrintMessage (Message msg)
	{
		Console.WriteLine ("Message (" + msg.Header.Endianness + " endian, v" + msg.Header.MajorVersion + "):");
		Console.WriteLine (indent + "Type: " + msg.Header.MessageType);
		Console.WriteLine (indent + "Flags: " + msg.Header.Flags);
		Console.WriteLine (indent + "Serial: " + msg.Header.Serial);

		//foreach (HeaderField hf in msg.HeaderFields)
		//	Console.WriteLine (indent + hf.Code + ": " + hf.Value);
		Console.WriteLine (indent + "Header Fields:");
		foreach (KeyValuePair<FieldCode,object> field in msg.Header.Fields)
			Console.WriteLine (indent + indent + field.Key + ": " + field.Value);

		Console.WriteLine (indent + "Body (" + msg.Header.Length + " bytes):");
		if (msg.Body != null) {
			MessageReader reader = new MessageReader (msg);

			//TODO: this needs to be done more intelligently
			//TODO: number the args
			try {
				foreach (DType dtype in msg.Signature.GetBuffer ()) {
					if (dtype == DType.Invalid)
						continue;
					object arg = reader.ReadValue (dtype);
					Console.WriteLine (indent + indent + dtype + ": " + arg);
				}
			} catch {
				Console.WriteLine (indent + indent + "monitor is too dumb to decode message body");
			}
		}
	}
}
