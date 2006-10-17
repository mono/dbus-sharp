// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using NDesk.DBus;
using org.freedesktop.DBus;

public class ManagedDBusTestObjects
{
	public static void Main ()
	{
		Bus bus = Bus.Session;

		ObjectPath myPath = new ObjectPath ("/org/ndesk/test");
		string myName = "org.ndesk.test";

		//TODO: write the rest of this demo and implement
	}
}

public class Device : IDevice
{
	public string GetName ()
	{
		return "Some device";
	}
}

public class DeviceManager : IDeviceManager
{
	public IDevice GetCurrentDevice ()
	{
		return new Device ();
	}
}

public interface IDevice
{
	string GetName ();
}

public interface IDeviceManager
{
	IDevice GetCurrentDevice ();
}

public interface IUglyDeviceManager
{
	ObjectPath GetCurrentDevice ();
}
