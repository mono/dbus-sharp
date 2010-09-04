// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using NUnit.Framework;
using DBus;
using DBus.Authentication;

namespace DBus.Tests
{
	[TestFixture]
	public class AuthenticationTest
	{
		[Test]
		public void AuthSelf ()
		{
			SaslServer server = new SaslServer ();
			SaslClient client = new SaslClient ();

			server.Peer = client;
			client.Peer = server;

			client.Identity = "1000";
			server.Guid = UUID.Generate ();

			Assert.IsTrue (client.AuthenticateSelf ());
			Assert.AreEqual (server.Guid, client.ActualId);
		}
	}
}
