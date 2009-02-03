// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NUnit.Framework;
using NDesk.DBus;

namespace NDesk.DBus.Tests
{
	[TestFixture]
	public class ObjectPathTest
	{
		[Test]
		public void Equality ()
		{
			string pathText = "/org/freedesktop/DBus";

			ObjectPath a = new ObjectPath (pathText);
			ObjectPath b = new ObjectPath (pathText);

			Assert.IsTrue (a.Equals (b));
			Assert.AreEqual (String.Empty.CompareTo (null), a.CompareTo (null));
			Assert.IsTrue (a == b);
			Assert.IsFalse (a != b);

			ObjectPath c = new ObjectPath (pathText + "/foo");
			Assert.IsFalse (a == c);
		}

		[Test]
		[ExpectedException]
		public void NullConstructor ()
		{
			new ObjectPath (null);
		}
	}
}
