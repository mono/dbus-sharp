// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NUnit.Framework;
using DBus;
using System.Linq;
using DBus.Protocol;

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
		
		[Test]
		public void CombineSignatures ()
		{
			var x = Signature.ByteSig + Signature.StringSig;
			Assert.AreEqual ("ys", x.Value, "#1");
		}
		
		[Test]
		public void MakeArray ()
		{
			var x = Signature.MakeArray (Signature.Int32Sig);
			Assert.AreEqual ("ai", x.Value, "#1");
		}
		
		[Test]
		public void MakeArrayOfStruct ()
		{
			var type = Signature.MakeStruct (Signature.Int32Sig + Signature.Int32Sig);
			var x = Signature.MakeArray (type);
			Assert.AreEqual ("a(ii)", x.Value, "#1");
		}
		
		[Test]
		public void MakeArrayOfArray ()
		{
			var x = Signature.MakeArray (Signature.Int32Sig);
			x = Signature.MakeArray (x);
			Assert.AreEqual ("aai", x.Value, "#1");
		}
		
		[Test]
		[ExpectedException (typeof (ArgumentException))]
		public void MakeArray_NotSingleCompleteType ()
		{
			Signature.MakeArray (Signature.Int32Sig + Signature.UInt16Sig);
		}
		
		[Test]
		public void MakeStruct ()
		{
			// 'r' isn't used, just brackets.
			var x = Signature.MakeStruct (Signature.ByteSig + Signature.StringSig);
			Assert.AreEqual ("(ys)", x.Value, "#1");
		}
		
		[Test]
		[ExpectedException (typeof (ArgumentException))]
		public void MakeStruct_Empty ()
		{
			Signature.MakeStruct (Signature.Empty);
		}
		
		[Test]
		public void MakeDictionaryEntry ()
		{
			// Make a valid dictionary entry, should appear as an array of dict_entries
			var x = Signature.MakeDictEntry (Signature.StringSig, Signature.Int32Sig);
			Assert.AreEqual ("{si}", x.Value, "#1");
		}
		
		[Test]
		public void MakeDictionary ()
		{
			// 'r' isn't used, just brackets.
			var x = Signature.MakeDict (Signature.StringSig, Signature.Int32Sig);
			Assert.AreEqual ("a{si}", x.Value, "#1");
		}
		
		[Test]
		[ExpectedException (typeof (ArgumentException))]
		public void MakeDictionary_TwoCompleteTypes_Key ()
		{
			// They key is not a single complete type
			 Signature.MakeDictEntry (Signature.StringSig + Signature.Int32Sig, Signature.Int32Sig);
		}
		
		[Test]
		[ExpectedException (typeof (ArgumentException))]
		public void MakeDictionary_TwoCompleteTypes_Value ()
		{
			// They value is not a single complete type
			Signature.MakeDictEntry (Signature.StringSig, Signature.Int32Sig + Signature.Int32Sig);
		}
	}
}
