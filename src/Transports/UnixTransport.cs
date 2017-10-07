// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

//We send BSD-style credentials on all platforms
//Doesn't seem to break Linux (but is redundant there)
//This may turn out to be a bad idea
#define HAVE_CMSGCRED

using System;
using System.IO;

using DBus.Protocol;
using DBus.Unix;

namespace DBus.Transports
{
	abstract class UnixTransport : Transport
	{
		public override void Open (AddressEntry entry)
		{
			string path;
			bool abstr;

			if (entry.Properties.TryGetValue ("path", out path))
				abstr = false;
			else if (entry.Properties.TryGetValue ("abstract", out path))
				abstr = true;
			else
				throw new ArgumentException ("No path specified for UNIX transport");

			Open (path, abstr);
		}

		public abstract void Open (string path, bool @abstract);

		//send peer credentials null byte
		//different platforms do this in different ways
#if HAVE_CMSGCRED
		unsafe void WriteBsdCred ()
		{
			//null credentials byte
			byte buf = 0;

			IOVector iov = new IOVector ();
			//iov.Base = (IntPtr)(&buf);
			iov.Base = &buf;
			iov.Length = 1;

			msghdr msg = new msghdr ();
			msg.msg_iov = &iov;
			msg.msg_iovlen = 1;

			cmsg cm = new cmsg ();
			msg.msg_control = (IntPtr)(&cm);
			msg.msg_controllen = (uint)sizeof (cmsg);
			cm.hdr.cmsg_len = (uint)sizeof (cmsg);
			cm.hdr.cmsg_level = 0xffff; //SOL_SOCKET
			cm.hdr.cmsg_type = 0x03; //SCM_CREDS

			int written = new UnixSocket ((int) SocketHandle, false).SendMsg (&msg, 0);
			if (written != 1)
				throw new Exception ("Failed to write credentials");
		}
#endif

		public override void WriteCred ()
		{
#if HAVE_CMSGCRED
			try {
				WriteBsdCred ();
				return;
			} catch {
				if (ProtocolInformation.Verbose)
					Console.Error.WriteLine ("Warning: WriteBsdCred() failed; falling back to ordinary WriteCred()");
			}
#endif
			//null credentials byte
			byte buf = 0;
			Stream.WriteByte (buf);
		}
	}

#if HAVE_CMSGCRED
	unsafe struct msghdr
	{
		public IntPtr msg_name; //optional address
		public uint msg_namelen; //size of address
		public IOVector *msg_iov; //scatter/gather array
		public int msg_iovlen; //# elements in msg_iov
		public IntPtr msg_control; //ancillary data, see below
		public uint msg_controllen; //ancillary data buffer len
		public int msg_flags; //flags on received message
	}

	struct cmsghdr
	{
		public uint cmsg_len; //data byte count, including header
		public int cmsg_level; //originating protocol
		public int cmsg_type; //protocol-specific type
	}

	unsafe struct cmsgcred
	{
		const int CMGROUP_MAX = 16;

		public int cmcred_pid; //PID of sending process
		public uint cmcred_uid; //real UID of sending process
		public uint cmcred_euid; //effective UID of sending process
		public uint cmcred_gid; //real GID of sending process
		public short cmcred_ngroups; //number or groups
		public fixed uint cmcred_groups[CMGROUP_MAX]; //groups
	}

	struct cmsg
	{
		public cmsghdr hdr;
		public cmsgcred cred;
	}
#endif
}
