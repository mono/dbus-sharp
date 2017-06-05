// Copyright 2017 Steffen Kiess <kiess@ki4.de>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

using DBus.Protocol;

using Mono.Unix;
using Mono.Unix.Native;

namespace DBus.Transports
{
	class UnixSendmsgTransport : UnixTransport
	{
		static bool CheckAvailable ()
		{
			var msghdr = new Msghdr {
				msg_iov = new Iovec[] {},
				msg_iovlen = 0,
			};
			// sendmsg() should return EBADFD because fd == -1
			// If sendmsg() is not available (e.g. because Mono.Posix is too
			// old or this is on a system without sendmsg()), Syscall.sendmsg()
			// will throw an exception
			Syscall.sendmsg (-1, msghdr, 0);
			return true;
		}

		public static bool Available ()
		{
			try {
				return CheckAvailable ();
			} catch {
				return false;
			}
		}

		public UnixSendmsgTransport ()
		{
			SocketHandle = -1;
		}

		internal override bool TransportSupportsUnixFD { get { return true; } }

		public override string AuthString ()
		{
			long uid = Mono.Unix.Native.Syscall.geteuid ();
			return uid.ToString ();
		}

		public override void Open (string path, bool @abstract)
		{
			if (String.IsNullOrEmpty (path))
				throw new ArgumentException ("path");

			var addr = new SockaddrUn (path, linuxAbstractNamespace:@abstract);

			SocketHandle = Syscall.socket (UnixAddressFamily.AF_UNIX, UnixSocketType.SOCK_STREAM, 0);
			if (SocketHandle == -1)
				UnixMarshal.ThrowExceptionForLastError ();
			bool success = false;
			try {
				if (Syscall.connect ((int) SocketHandle, addr) < 0)
					UnixMarshal.ThrowExceptionForLastError ();
				Stream = new DBus.Unix.UnixMonoStream ((int) SocketHandle);
				success = true;
			} finally {
				if (!success) {
					int ret = Syscall.close ((int) SocketHandle);
					SocketHandle = -1;
					if (ret == -1)
						UnixMarshal.ThrowExceptionForLastError ();
				}
			}
		}

		readonly object writeLock = new object ();

		byte[] cmsgBuffer = new byte[Syscall.CMSG_LEN ((ulong)(sizeof (int) * UnixFDArray.MaxFDs))];
		// Might return short reads
		unsafe int ReadShort (byte[] buffer, int offset, int length, UnixFDArray fdArray)
		{
			if (length < 0 || offset < 0 || length + offset < length || length + offset > buffer.Length)
				throw new ArgumentException ();

			fixed (byte* ptr = buffer, cmsgPtr = cmsgBuffer) {
				var iovecs = new Iovec[] {
					new Iovec {
						iov_base = (IntPtr) (ptr + offset),
						iov_len = (ulong) length,
					},
				};

				var msghdr = new Msghdr {
					msg_iov = iovecs,
					msg_iovlen = 1,
					msg_control = cmsgBuffer,
					msg_controllen = cmsgBuffer.Length,
				};

				long r;
				do {
					r = Syscall.recvmsg ((int) SocketHandle, msghdr, 0);
				} while (UnixMarshal.ShouldRetrySyscall ((int) r));

				for (long cOffset = Syscall.CMSG_FIRSTHDR (msghdr); cOffset != -1; cOffset = Syscall.CMSG_NXTHDR (msghdr, cOffset)) {
					var recvHdr = Cmsghdr.ReadFromBuffer (msghdr, cOffset);
					if (recvHdr.cmsg_level != UnixSocketProtocol.SOL_SOCKET)
						continue;
					if (recvHdr.cmsg_type != UnixSocketControlMessage.SCM_RIGHTS)
						continue;
					var recvDataOffset = Syscall.CMSG_DATA (msghdr, cOffset);
					var bytes = recvHdr.cmsg_len - (recvDataOffset - cOffset);
					var fdCount = bytes / sizeof (int);
					for (int i = 0; i < fdCount; i++)
						fdArray.FDs.Add (new UnixFD (((int*) (cmsgPtr + recvDataOffset))[i]));
				}

				if ((msghdr.msg_flags & MessageFlags.MSG_CTRUNC) != 0)
					throw new Exception ("Control message truncated (probably file descriptors lost)");

				return (int) r;
			}
		}

		internal override int Read (byte[] buffer, int offset, int length, UnixFDArray fdArray)
		{
			if (!Connection.UnixFDSupported || fdArray == null)
				return base.Read (buffer, offset, length, fdArray);

			int read = 0;
			while (read < length) {
				int nread = ReadShort (buffer, offset + read, length - read, fdArray);
				if (nread == 0)
					break;
				read += nread;
			}

			if (read > length)
				throw new Exception ();

			return read;
		}

		internal override unsafe void WriteMessage (Message msg)
		{
			if (msg.UnixFDArray == null || msg.UnixFDArray.FDs.Count == 0) {
				base.WriteMessage (msg);
				return;
			}
			if (!Connection.UnixFDSupported)
				throw new Exception ("Attempting to write Unix FDs to a connection which does not support them");

			lock (writeLock) {
				var ms = new MemoryStream ();
				msg.Header.GetHeaderDataToStream (ms);
				var header = ms.ToArray ();
				((DBus.Unix.UnixMonoStream) Stream).Sendmsg (header, 0, header.Length, msg.Body, 0, msg.Body == null ? 0 : msg.Body.Length, msg.UnixFDArray);
			}
		}
	}
}

// vim: noexpandtab
// Local Variables:
// tab-width: 4
// c-basic-offset: 4
// indent-tabs-mode: t
// End:
