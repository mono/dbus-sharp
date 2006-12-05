// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NDesk.DBus;
using NDesk.DBus.Transports;
using org.freedesktop.DBus;

using System.IO;
using System.Net;
using System.Net.Sockets;

using System.Threading;

public class TestServerTcp
{
	public static void Main (string[] args)
	{
		bool isServer;

		int port;
		string hostname = "127.0.0.1";
		//IPAddress ipaddr = IPAddress.Parse ("127.0.0.1");

		if (args.Length == 2 && args[0] == "server") {
			isServer = true;
			port = Int32.Parse (args[1]);
		} else if (args.Length == 3 && args[0] == "client") {
			isServer = false;
			hostname = args[1];
			port = Int32.Parse (args[2]);
		} else {
			Console.Error.WriteLine ("Usage: test-server-tcp [server PORT|client HOSTNAME PORT]");
			return;
		}

		Connection conn;

		ObjectPath myOpath = new ObjectPath ("/org/ndesk/test");
		string myNameReq = "org.ndesk.test";

		if (!isServer) {
			SocketTransport transport = new SocketTransport ();
			transport.Open (hostname, port);
			conn = new Connection (transport);

			DemoObject demo = conn.GetObject<DemoObject> (myNameReq, myOpath);
			demo.GiveNoReply ();
			//float ret = demo.Hello ("hi from test client", 21);
			float ret = 200;
			while (ret > 5) {
				ret = demo.Hello ("hi from test client", (int)ret);
				Console.WriteLine ("Returned float: " + ret);
				System.Threading.Thread.Sleep (1000);
			}
		} else {
			TcpListener server = new TcpListener (IPAddress.Any, port);

			server.Start ();

			while (true) {
				Console.WriteLine ("Waiting for client on " + port);
				TcpClient client = server.AcceptTcpClient ();
				Console.WriteLine ("Client accepted");

				//TODO: use the right abstraction here, probably using the Server class
				SocketTransport transport = new SocketTransport ();
				conn = new Connection (transport);
				conn.ns = client.GetStream ();

				//conn.SocketHandle = (long)clientSocket.Handle;

				//ConnectionHandler.Handle (conn);

				//in reality a thread per connection is of course too expensive
				ConnectionHandler hnd = new ConnectionHandler (conn);
				new Thread (new ThreadStart (hnd.Handle)).Start ();

				Console.WriteLine ();
			}
		}
	}
}

public class ConnectionHandler
{
	protected Connection conn;

	public ConnectionHandler (Connection conn)
	{
		this.conn = conn;
	}

	public void Handle ()
	{
		ConnectionHandler.Handle (conn);
	}

	public static void Handle (Connection conn)
	{
		string myNameReq = "org.ndesk.test";
		ObjectPath myOpath = new ObjectPath ("/org/ndesk/test");

		DemoObject demo = new DemoObject ();
		conn.Register (myNameReq, myOpath, demo);

		//TODO: handle lost connections etc. properly instead of stupido try/catch
		try {
		while (true)
			conn.Iterate ();
		} catch (Exception e) {
			//Console.Error.WriteLine (e);
		}

		conn.Unregister (myNameReq, myOpath);
	}
}

[Interface ("org.ndesk.test")]
public class DemoObject : MarshalByRefObject
{
	public float Hello (string arg0, int arg1)
	{
		Console.WriteLine ("Got a Hello(" + arg0 + ", " + arg1 +")");

		return (float)arg1/2;
	}

	public void GiveNoReply ()
	{
		Console.WriteLine ("Asked to give no reply");
	}
}
