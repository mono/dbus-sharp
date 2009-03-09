// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using NDesk.DBus;
using org.freedesktop.DBus;

public class ManagedDBusTestRental
{
	public static void Main ()
	{
		Bus bus = Bus.Session;

		string bus_name = "org.ndesk.test";
		ObjectPath path = new ObjectPath ("/org/ndesk/test");
		ObjectPath cppath = new ObjectPath ("/org/ndesk/CodeProvider");

		IDemoOne demo;

		if (bus.RequestName (bus_name) == RequestNameReply.PrimaryOwner) {
			//create a new instance of the object to be exported
			demo = new Demo ();
			bus.Register (path, demo);

			DCodeProvider dcp = new DCodeProvider();
			bus.Register (cppath, dcp);

			//run the main loop
			while (true)
				bus.Iterate ();

		} else {
			//import a remote to a local proxy
			//demo = bus.GetObject<IDemo> (bus_name, path);
			demo = bus.GetObject<DemoProx> (bus_name, path);
			//RunTest (demo);

			ICodeProvider idcp = bus.GetObject<ICodeProvider> (bus_name, cppath);

			DMethodInfo dmi = idcp.GetMethod ("DemoProx", "SayRepeatedly");

			//DynamicMethod dm = new DynamicMethod (dmi.Name, typeof(void), new Type[] { typeof(object), typeof(int), typeof(string) }, typeof(DemoBase));
			//ILGenerator ilg = dm.GetILGenerator ();
			//dmi.Implement (ilg);

			DynamicMethod dm = dmi.GetDM ();

			SayRepeatedlyHandler cb = (SayRepeatedlyHandler)dm.CreateDelegate (typeof (SayRepeatedlyHandler), demo);
			int retVal;
			retVal = cb (12, "Works!");
			Console.WriteLine("retVal: " + retVal);

			/*
			for (int i = 0 ; i != dmi.Code.Length ; i++) {
				if (!dmi.Code[i].Emit(ilg))
					throw new Exception(String.Format("Code gen failure at i={0} {1}", i, dmi.Code[i].opCode));
			}
			*/
			//SayRepeatedlyHandler
		}
	}

	public static void RunTest (IDemoOne demo)
	{
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

		Console.WriteLine("SomeProp: " + demo.SomeProp);
		demo.SomeProp = 321;

		DemoProx demoProx = demo as DemoProx;
		if (demoProx != null) {
			//demoProx.SayRepeatedly(5, "Repetition");
			//demoProx.GetType().InvokeMember("RepProx", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, null, new object[] {demoProx, 5, "Lala"});
			//demoProx.GetType().GetMethod("RepProx").Invoke(null, new object[] {demoProx, 5, "Lala"});
			demoProx.GetType().GetMethod("RepProx", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(null, new object[] {demoProx, 5, "Lala"});
			//demoProx.GetType().InvokeMember("RepProx", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, demoProx, new object[] {5, "Lala"});
		}

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

public delegate int SayRepeatedlyHandler (int count, string str);

[Interface ("org.ndesk.CodeProvider")]
public interface ICodeProvider
{
	DMethodInfo GetMethod (string iface, string name);
}

public struct DMethodBody
{
	public string[] Locals;
	public ILReader2.ILOp[] Code;

	public void DeclareLocals(ILGenerator ilg)
	{
		foreach (string local in Locals) {
			Type t;
			if (!ILReader2.TryGetType(local, out t))
				break;
			ilg.DeclareLocal(t);
		}
	}
}

public struct DMethodInfo
{
	public string Name;
	//public DTypeInfo DeclaringType;
	public DTypeInfo[] Parameters;
	//public DArgumentInfo[] Arguments;
	public DTypeInfo ReturnType;
	public DMethodBody Body;

	public void Implement(ILGenerator ilg)
	{
		DMethodBody body = Body;

		body.DeclareLocals(ilg);

		for (int i = 0 ; i != body.Code.Length ; i++) {
			if (!body.Code[i].Emit(ilg))
				throw new Exception(String.Format("Code gen failure at i={0} {1}", i, body.Code[i].opCode));
		}
	}

	public DynamicMethod GetDM ()
	{
		List<Type> parms = new List<Type>();
		parms.Add(typeof(object));
		foreach (DTypeInfo dti in Parameters)
			parms.Add(dti.ToType());

		DynamicMethod dm = new DynamicMethod (Name, ReturnType.ToType(), parms.ToArray(), typeof(DemoBase));

		ILGenerator ilg = dm.GetILGenerator();
		Implement(ilg);
		return dm;
	}
}

public enum DArgumentDirection
{
	In,
	Out,
}

public struct DArgumentInfo
{
	public DArgumentInfo(DTypeInfo argType) : this(String.Empty, argType)
	{
	}

	public DArgumentInfo(string name, DTypeInfo argType) : this(name, argType, DArgumentDirection.In)
	{
	}

	public DArgumentInfo(string name, DTypeInfo argType, DArgumentDirection direction)
	{
		this.Name = name;
		this.ArgType = argType;
		this.Direction = direction;
	}

	public string Name;
	public DTypeInfo ArgType;
	public DArgumentDirection Direction;
}

public struct DTypeInfo
{
	public DTypeInfo(string name)
	{
		this.Name = name;
	}

	public Type ToType()
	{
		Type t;
		if (!ILReader2.TryGetType(Name, out t))
			return null;
		return t;
	}

	public string Name;
	//public DMethodInfo[] Methods;
}

public class DCodeProvider : ICodeProvider
{
	public DMethodInfo GetMethod (string iface, string name)
	{
		DMethodInfo dmi = new DMethodInfo ();

		dmi.Name = name;
		//dmi.Code = new byte[0];

		Type declType = typeof(DemoProx);
		MethodInfo mi = declType.GetMethod(name);

		List<string> locals = new List<string>();
		MethodBody body = mi.GetMethodBody ();
		foreach (LocalVariableInfo lvar in body.LocalVariables)
			locals.Add(lvar.LocalType.FullName);
		dmi.Body.Locals = locals.ToArray();

		List<DTypeInfo> parms = new List<DTypeInfo>();
		foreach (ParameterInfo parm in mi.GetParameters())
			parms.Add(new DTypeInfo(parm.ParameterType.FullName));
		dmi.Parameters = parms.ToArray();

		dmi.ReturnType = new DTypeInfo(mi.ReturnType.FullName);

		ILReader2 ilr = new ILReader2(mi);
		dmi.Body.Code = ilr.Iterate();

		return dmi;
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
	public virtual int SayRepeatedly (int count, string str)
	{
		for (int i = 0 ; i != count ; i++)
			//Say2("Woo! " + str);
			Say2(str);
			//Console.WriteLine("This is a local CWL");
			//Say2("FIXED");
		//Console.WriteLine();
		//Console.WriteLine(str);
		//
		/*
		{
			string fa = "lala";
			fa += "bar";
			Say2(fa);
		}
		*/
		return 12;
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
	public string A;
	public string B;
}

public delegate void SomeEventHandler (string arg1, object arg2, double arg3, MyTuple mt);
