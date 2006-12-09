// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NDesk.DBus;
using org.freedesktop.DBus;

using System.IO;
using Mono.Unix;

using System.Threading;

using System.Text;
using NDesk.DBus.Transports;

public class TestServerNative
{
	//TODO: complete this test daemon/server example, and a client
	//TODO: maybe generalise it and integrate it into the core
	public static void Main (string[] args)
	{
		bool isServer;

		if (args.Length == 1 && args[0] == "server")
			isServer = true;
		else if (args.Length == 1 && args[0] == "client")
			isServer = false;
		else {
			Console.Error.WriteLine ("Usage: test-server [server|client]");
			return;
		}

		//string addr = "unix:abstract=/tmp/dbus-ABCDEFGHIJ";
		string addr = "unix:path=/tmp/dbus-ABCDEFGHIJ";

		Connection conn;

		ObjectPath myOpath = new ObjectPath ("/org/ndesk/test");
		string myNameReq = "org.ndesk.test";

		if (!isServer) {
			conn = new Connection (Transport.Create (AddressEntry.Parse (addr)));
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
			string path;
			bool abstr;

			AddressEntry entry = AddressEntry.Parse (addr);
			path = entry.Properties["path"];

			UnixSocket server = new UnixSocket ();


			byte[] p = Encoding.Default.GetBytes (path);

			byte[] sa = new byte[2 + p.Length + 1];

			//we use BitConverter to stay endian-safe
			byte[] afData = BitConverter.GetBytes (UnixSocket.AF_UNIX);
			sa[0] = afData[0];
			sa[1] = afData[1];

			for (int i = 0 ; i != p.Length ; i++)
				sa[2 + i] = p[i];
			sa[2 + p.Length] = 0; //null suffix for domain socket addresses, see unix(7)


			server.Bind (sa);
			//server.Listen (1);
			server.Listen (5);

			while (true) {
				Console.WriteLine ("Waiting for client on " + addr);
				UnixSocket client = server.Accept ();
				Console.WriteLine ("Client accepted");
				//client.Blocking = true;

				//PeerCred pc = new PeerCred (client);
				//Console.WriteLine ("PeerCred: pid={0}, uid={1}, gid={2}", pc.ProcessID, pc.UserID, pc.GroupID);

				UnixNativeTransport transport = new UnixNativeTransport ();
				transport.Stream = new UnixStream (client.Handle);
				conn = new Connection (transport);

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
