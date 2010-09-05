// Copyright 2009 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NUnit.Framework;
using DBus;

namespace DBus.Tests
{
	[TestFixture]
	public class ObjectPathTest
	{
		[Test]
		[ExpectedException (typeof (ArgumentException))]
		public void InvalidStartingCharacter ()
		{
			// Paths must start with "/"
			new ObjectPath ("no_starting_slash");
		}

		[Test]
		[ExpectedException (typeof (ArgumentException))]
		public void InvalidEndingCharacter ()
		{
			// Paths must not end with "/"
			new ObjectPath ("/ends_with_slash/");
		}

		[Test]
		[ExpectedException (typeof (ArgumentException))]
		public void InvalidCharacters ()
		{
			// Paths must be in the range "[A-Z][a-z][0-9]_"
			new ObjectPath ("/?valid/path/invalid?/character.^");
		}

		[Test]
		public void MultipleSequentialSlashes ()
		{
			// Multiple sequential '/' chars are not allowed
			new ObjectPath ("/test//fail");
		}

		[Test]
		public void ConstructorTest ()
		{
			var x = new ObjectPath ("/");
			Assert.AreEqual (x.ToString (), "/", "#1");
			Assert.AreEqual (x, ObjectPath.Root, "#2");

			x = new ObjectPath ("/this/01234567890/__Test__");
			Assert.AreEqual ("/this/01234567890/__Test__", x.ToString (), "#3");
		}

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
		[ExpectedException (typeof (ArgumentNullException))]
		public void NullConstructor ()
		{
			new ObjectPath (null);
		}

		[Test]
		[ExpectedException (typeof (ArgumentException))]
		public void EmptyStringConstructor ()
		{
			new ObjectPath ("");
		}
	}
}
