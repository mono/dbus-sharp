// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NUnit.Framework;
using NDesk.DBus;

namespace NDesk.DBus.Tests
{
	[TestFixture]
	public class SignatureTest
	{
		[Test]
		public void Parse ()
		{
			string sigText = "as";
			Signature sig = new Signature (sigText);

			Assert.IsTrue (sig.IsArray);
			Assert.IsFalse (sig.IsDict);
			Assert.IsFalse (sig.IsPrimitive);
		}

		[Test]
		public void Equality ()
		{
			string sigText = "as";
			Signature a = new Signature (sigText);
			Signature b = new Signature (sigText);

			Assert.IsTrue (a == b);
		}
	}
}
