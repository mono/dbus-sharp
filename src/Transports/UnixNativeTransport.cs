// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

using DBus.Unix;
using DBus.Protocol;

namespace DBus.Transports
{
	class UnixNativeTransport : UnixTransport
	{
		internal UnixSocket socket;

		public override string AuthString ()
		{
			long uid = Mono.Unix.Native.Syscall.geteuid ();
			return uid.ToString ();
		}

		public override void Open (string path, bool @abstract)
		{
			if (String.IsNullOrEmpty (path))
				throw new ArgumentException ("path");

			if (@abstract)
				socket = OpenAbstractUnix (path);
			else
				socket = OpenUnix (path);

			//socket.Blocking = true;
			SocketHandle = (long)socket.Handle;
			//Stream = new UnixStream ((int)socket.Handle);
			Stream = new UnixStream (socket);
		}

		public static byte[] GetSockAddr (string path)
		{
			byte[] p = Encoding.Default.GetBytes (path);

			byte[] sa = new byte[2 + p.Length + 1];

			//we use BitConverter to stay endian-safe
			byte[] afData = BitConverter.GetBytes (UnixSocket.AF_UNIX);
			sa[0] = afData[0];
			sa[1] = afData[1];

			for (int i = 0 ; i != p.Length ; i++)
				sa[2 + i] = p[i];
			sa[2 + p.Length] = 0; //null suffix for domain socket addresses, see unix(7)

			return sa;
		}

		public static byte[] GetSockAddrAbstract (string path)
		{
			byte[] p = Encoding.Default.GetBytes (path);

			byte[] sa = new byte[2 + 1 + p.Length];

			//we use BitConverter to stay endian-safe
			byte[] afData = BitConverter.GetBytes (UnixSocket.AF_UNIX);
			sa[0] = afData[0];
			sa[1] = afData[1];

			sa[2] = 0; //null prefix for abstract domain socket addresses, see unix(7)
			for (int i = 0 ; i != p.Length ; i++)
				sa[3 + i] = p[i];

			return sa;
		}

		internal UnixSocket OpenUnix (string path)
		{
			byte[] sa = GetSockAddr (path);
			UnixSocket client = new UnixSocket ();
			client.Connect (sa);
			return client;
		}

		internal UnixSocket OpenAbstractUnix (string path)
		{
			byte[] sa = GetSockAddrAbstract (path);
			UnixSocket client = new UnixSocket ();
			client.Connect (sa);
			return client;
		}
	}
}
