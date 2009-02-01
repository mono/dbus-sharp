// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NUnit.Framework;
using NDesk.DBus;

namespace NDesk.DBus.Tests
{
	[TestFixture]
	public class AddressTest
	{
		[Test]
		[ExpectedException (typeof (BadAddressException))]
		public void ParseBad ()
		{
			Address.Parse ("lala");
		}

		[Test]
		public void ParseUnix ()
		{
			string addressText = @"unix:path=/var/run/dbus/system_bus_socket";

			AddressEntry[] addrs = Address.Parse (addressText);
			Assert.AreEqual (1, addrs.Length);

			AddressEntry entry = addrs[0];
			Assert.AreEqual (addressText, entry.ToString ());

			Assert.AreEqual ("unix", entry.Method);
			Assert.AreEqual (1, entry.Properties.Count);
			Assert.AreEqual ("/var/run/dbus/system_bus_socket", entry.Properties["path"]);
		}

		[Test]
		public void ParseMany ()
		{
			string addressText = @"unix:path=/var/run/dbus/system_bus_socket;unix:path=/var/run/dbus/system_bus_socket";

			AddressEntry[] addrs = Address.Parse (addressText);
			Assert.AreEqual (2, addrs.Length);
			// TODO: Improve this test.
		}
	}
}
