// Copyright 2017 Steffen Kiess <kiess@ki4.de>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;

using Mono.Unix;
using Mono.Unix.Native;

namespace DBus
{
	public class UnixFD : IDisposable
	{
		object lck = new object ();
		int fd = -1;

		public UnixFD (int fd)
		{
			this.fd = fd;
		}

		public void Dispose ()
		{
			lock (lck) {
				if (fd != -1) {
					int r;
					// Don't retry close() on EINTR, on a lot of systems (e.g. Linux) the FD will be already closed when EINTR is returned, see https://lwn.net/Articles/576478/
					r = DBus.Unix.UnixSocket.close (fd);
					fd = -1;

					if (r < 0)
						UnixMarshal.ThrowExceptionForLastError ();
				}
			}
		}

		public override string ToString ()
		{
			lock (lck) {
				if (fd == -1)
					return "UnixFD (disposed)";
				return "UnixFD (" + fd + ")";
			}
		}

		// The caller has to make sure that the FD does not get closed between
		// calling UnixFD.Handle and using the handle
		public int Handle {
			get {
				lock (lck) {
					if (fd == -1)
						throw new ObjectDisposedException (GetType ().FullName);
					return fd;
				}
			}
		}

		// Return a new UnixFD instance which will be usable after this UnixFD
		// has been closed
		public UnixFD Clone () {
			lock (lck) {
				if (fd == -1)
					throw new ObjectDisposedException (GetType ().FullName);
				int newFd = Syscall.dup (fd);
				if (newFd < 0)
					UnixMarshal.ThrowExceptionForLastError ();
				return new UnixFD (newFd);
			}
		}
	}
}
