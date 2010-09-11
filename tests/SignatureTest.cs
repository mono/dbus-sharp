// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NUnit.Framework;
using DBus;
using System.Linq;

namespace DBus.Tests
{
	[TestFixture]
	public class SignatureTest
	{
		[Test]
		[ExpectedException (typeof (ArgumentNullException))]
		public void Parse_NullString ()
		{
			new Signature ((string) null);
		}
		
		[Test]
		[ExpectedException (typeof (ArgumentNullException))]
		public void Parse_NullArray ()
		{
			new Signature ((DType []) null);
		}
		
		[Test]
		public void Parse_Empty ()
		{
			var x = new Signature ("");
			Assert.AreEqual (Signature.Empty, x, "#1");
		}
		
		[Test]
		public void ParseStruct ()
		{
			var sig = new Signature ("(iu)");
			Assert.IsTrue (sig.IsStruct, "#1");
			
			var elements = sig.GetFieldSignatures ().ToArray ();
			Assert.AreEqual (2, elements.Length, "#2");
			Assert.AreEqual (Signature.Int32Sig, elements [0], "#3");
			Assert.AreEqual (Signature.UInt32Sig, elements [1], "#4");
		}
		
		[Test]
		[ExpectedException (typeof (ArgumentException))]
		public void ParseInvalid_TypeCode ()
		{
			// Use an invalid type code
			new Signature ("z");
		}
		
		[Test]
		[ExpectedException (typeof (ArgumentException))]
		[Ignore ("Not implemented yet")]
		public void ParseInvalid_MissingClosingBrace ()
		{
			// Use an invalid type code
			new Signature ("(i");
		}
		
		[Test]
		[ExpectedException (typeof (ArgumentException))]
		[Ignore ("Not implemented yet")]
		public void ParseInvalid_MissingOpeningBrace ()
		{
			// Use an invalid type code
			new Signature ("i)");
		}
		
		[Test]
		public void Parse_ArrayOfString ()
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
			Assert.IsTrue (a.GetElementSignature () == Signature.StringSig);

			Assert.AreEqual (a + b + Signature.Empty, new Signature ("asas"));
		}

		[Test]
		public void FixedSize ()
		{
			Signature sig;

			sig = new Signature ("s");
			Assert.IsFalse (sig.IsFixedSize);

			sig = new Signature ("as");
			Assert.IsFalse (sig.IsFixedSize);

			sig = new Signature ("u");
			Assert.IsTrue (sig.IsFixedSize);

			sig = new Signature ("u(ub)");
			Assert.IsTrue (sig.IsFixedSize);

			sig = new Signature ("u(uvb)");
			Assert.IsFalse (sig.IsFixedSize);
		}
	}
}
