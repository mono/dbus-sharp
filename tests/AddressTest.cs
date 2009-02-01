// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NUnit.Framework;
using NDesk.DBus;
using System.Collections.Generic;

namespace NDesk.DBus.Tests
{
	[TestFixture]
	public class AddressTest
	{
		[Test]
		[ExpectedException (typeof (InvalidAddressException))]
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
			Assert.AreEqual (UUID.Zero, entry.GUID);
		}

		[Test]
		public void ParseMany ()
		{
			string addressText = @"unix:path=/var/run/dbus/system_bus_socket;unix:path=/var/run/dbus/system_bus_socket";

			AddressEntry[] addrs = Address.Parse (addressText);
			Assert.AreEqual (2, addrs.Length);
			// TODO: Improve this test.
		}

		[Test]
		public void ParseGuid ()
		{
			string addressText = @"unix:abstract=/tmp/dbus-A4EzCUcGvg,guid=50ab33155e2cdd289e58c42a497ded1e";

			AddressEntry[] addrs = Address.Parse (addressText);
			Assert.AreEqual (1, addrs.Length);

			AddressEntry entry = addrs[0];
			Assert.AreEqual (addressText, entry.ToString ());

			Assert.AreEqual ("unix", entry.Method);
			Assert.AreEqual (1, entry.Properties.Count);

			UUID expectedId = UUID.Parse ("50ab33155e2cdd289e58c42a497ded1e");
			uint expectedTimestamp = 1232989470;
			Assert.AreEqual (expectedTimestamp, expectedId.UnixTimestamp);
			Assert.AreEqual (expectedId, entry.GUID);
		}

		[Test]
		public void UUIDEntropy ()
		{
			int n = 10000;
			DateTime dt = DateTime.MinValue;

			HashSet<int> hs = new HashSet<int> ();
			for (int i = 0 ; i != n ; i++)
				Assert.IsTrue (hs.Add (UUID.Generate (dt).GetHashCode ()));
		}
	}
}
