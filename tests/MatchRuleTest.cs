// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NUnit.Framework;
using DBus;
using DBus.Protocol;

namespace DBus.Tests
{
	[TestFixture]
	public class MatchRuleTest
	{
		[Test]
		public void Parse ()
		{
			string ruleText = @"member='Lala'";
			MatchRule rule = MatchRule.Parse (ruleText);

			Assert.AreEqual (MessageType.All, rule.MessageType);
			Assert.AreEqual (0, rule.Args.Count);
			Assert.AreEqual (ruleText, rule.ToString ());
		}

		[Test]
		public void ParsePathArgs ()
		{
			string ruleText = @"arg0='La',arg1path='/Foo'";
			MatchRule rule = MatchRule.Parse (ruleText);
			Assert.AreEqual (ruleText, rule.ToString ());
		}

		[Test]
		public void CanonicalOrdering ()
		{
			string ruleText = @"arg0='La',arg5path='/bar',arg2='Fa',destination='org.ndesk.Recipient',interface='org.ndesk.ITest',arg1path='/foo'";
			string sortedRuleText = @"interface='org.ndesk.ITest',destination='org.ndesk.Recipient',arg0='La',arg1path='/foo',arg2='Fa',arg5path='/bar'";
			MatchRule rule = MatchRule.Parse (ruleText);
			Assert.AreEqual (4, rule.Args.Count);
			Assert.AreEqual (sortedRuleText, rule.ToString ());
		}

		// TODO: Should fail
		/*
		[Test]
		public void ParseArgsPartiallyBad ()
		{
			string ruleText = @"arg0='A',arg4='Foo\'";
			MatchRule.Parse (ruleText);
		}
		*/

		// TODO: Should fail
		/*
		[Test]
		[ExpectedException]
		public void ParseRepeated ()
		{
			string ruleText = @"interface='org.ndesk.ITest',interface='org.ndesk.ITest2'";
			MatchRule.Parse (ruleText);
		}
		*/

		// TODO: Should fail
		/*
		[Test]
		//[ExpectedException]
		public void ParseArgsRepeated ()
		{
			string ruleText = @"arg0='A',arg0='A'";
			MatchRule.Parse (ruleText);
		}
		*/

		[Test]
		public void ParseArgsMaxAllowed ()
		{
			string ruleText = @"arg63='Foo'";
			MatchRule.Parse (ruleText);
		}

		[Test]
		[ExpectedException]
		public void ParseArgsMoreThanAllowed ()
		{
			string ruleText = @"arg64='Foo'";
			MatchRule.Parse (ruleText);
		}

		[Test]
		public void ParseArgs ()
		{
			string ruleText = @"arg5='F,o\'o\\\'\\',arg8=''";
			MatchRule rule = MatchRule.Parse (ruleText);

			Assert.AreEqual (MessageType.All, rule.MessageType);
			Assert.AreEqual (2, rule.Args.Count);

			//Assert.AreEqual (@"F,o'o\'\", rule.Args[5].Value);
			//Assert.AreEqual (@"", rule.Args[8].Value);

			Assert.AreEqual (ruleText, rule.ToString ());
		}
	}
}
