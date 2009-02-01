// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NUnit.Framework;
using NDesk.DBus;

namespace NDesk.DBus.Tests
{
	[TestFixture]
	public class MatchRuleTest
	{
		[Test]
		public void Parse ()
		{
			string ruleText = @"member='Lala'";
			MatchRule rule = MatchRule.Parse (ruleText);

			Assert.AreEqual (null, rule.MessageType);
			Assert.AreEqual (null, rule.Interface);
			Assert.AreEqual ("Lala", rule.Member);
			Assert.AreEqual (null, rule.Path);
			Assert.AreEqual (null, rule.Sender);
			Assert.AreEqual (null, rule.Destination);
			Assert.AreEqual (0, rule.Args.Count);

			Assert.AreEqual (ruleText, rule.ToString ());
		}

		// argNpath not yet supported
		/*
		[Test]
		[ExpectedException (typeof (FormatException))]
		public void ParseBadArgsPath ()
		{
			string ruleText = @"arg0path='Foo'";
			MatchRule.Parse (ruleText);
		}
		*/

		[Test]
		[ExpectedException (typeof (Exception))]
		public void ParseBadArgsMaxAllowed ()
		{
			string ruleText = @"arg64='Foo'";
			MatchRule.Parse (ruleText);
		}

		[Test]
		public void ParseArgsMaxAllowed ()
		{
			string ruleText = @"arg63='Foo'";
			MatchRule.Parse (ruleText);
		}

		[Test]
		public void ParseArgs ()
		{
			string ruleText = @"arg5='F,o\'o\\\'\\',arg8=''";
			MatchRule rule = MatchRule.Parse (ruleText);

			Assert.AreEqual (null, rule.MessageType);
			Assert.AreEqual (null, rule.Interface);
			Assert.AreEqual (null, rule.Member);
			Assert.AreEqual (null, rule.Path);
			Assert.AreEqual (null, rule.Sender);
			Assert.AreEqual (null, rule.Destination);
			Assert.AreEqual (2, rule.Args.Count);

			Assert.AreEqual (@"F,o'o\'\", rule.Args[5]);
			Assert.AreEqual (@"", rule.Args[8]);

			Assert.AreEqual (ruleText, rule.ToString ());
		}
	}
}
