// Copyright 2017 Steffen Kiess <kiess@ki4.de>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Runtime.InteropServices;

using Mono.Unix;
using Mono.Unix.Native;

namespace DBus.Unix
{
	sealed class UnixMonoStream : Stream
	{
		int fd;

		public UnixMonoStream (int fd)
		{
			this.fd = fd;
		}

		public override bool CanRead
		{
			get {
				return true;
			}
		}

		public override bool CanSeek
		{
			get {
				return false;
			}
		}

		public override bool CanWrite
		{
			get {
				return true;
			}
		}

		public override long Length
		{
			get {
				throw new NotImplementedException ("Seeking is not implemented");
			}
		}

		public override long Position
		{
			get {
				throw new NotImplementedException ("Seeking is not implemented");
			} set {
				throw new NotImplementedException ("Seeking is not implemented");
			}
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotImplementedException ("Seeking is not implemented");
		}

		public override void SetLength (long value)
		{
			throw new NotImplementedException ("Not implemented");
		}

		public override void Flush ()
		{
		}

		private void AssertValidBuffer (byte[] buffer, long offset, long length)
		{
			if (buffer == null)
				throw new ArgumentNullException ("buffer");
			if (offset < 0)
				throw new ArgumentOutOfRangeException ("offset", "< 0");
			if (length < 0)
				throw new ArgumentOutOfRangeException ("length", "< 0");
			if (offset > buffer.LongLength)
				throw new ArgumentException ("destination offset is beyond array size");
			if (offset > (buffer.LongLength - length))
				throw new ArgumentException ("would overrun buffer");
		}

		public override unsafe int Read (byte[] buffer, int offset, int length)
		{
			AssertValidBuffer (buffer, offset, length);

			long r = 0;
			fixed (byte* buf = buffer) {
				do {
					r = Syscall.read (fd, buf + offset, (ulong) length);
				} while (UnixMarshal.ShouldRetrySyscall ((int) r));
				if (r < 0)
					UnixMarshal.ThrowExceptionForLastError ();
				return (int) r;
			}
		}

		public override unsafe void Write (byte[] buffer, int offset, int length)
		{
			AssertValidBuffer (buffer, offset, length);

			int pos = 0;
			long r = 0;
			fixed (byte* buf = buffer) {
				while (pos < length) {
					do {
						r = Syscall.write (fd, buf + offset + pos, (ulong) length);
					} while (UnixMarshal.ShouldRetrySyscall ((int) r));
					if (r < 0)
						UnixMarshal.ThrowExceptionForLastError ();
					pos += (int) r;
				}
			}
		}

		public override void Close ()
		{
			if (fd != -1) {
				int ret = Syscall.close (fd);
				fd = -1;
				if (ret == -1)
					UnixMarshal.ThrowExceptionForLastError ();
			}
			base.Close ();
		}

		// Send the two buffers and the FDs using sendmsg(), don't handle short writes
		// length1 + length2 must not be 0
		public unsafe long SendmsgShort (byte[] buffer1, long offset1, long length1,
										byte[] buffer2, long offset2, long length2,
										DBus.Protocol.UnixFDArray fds)
		{
			//Console.WriteLine ("SendmsgShort (X, {0}, {1}, {2}, {3}, {4}, {5})", offset1, length1, buffer2 == null ? "-" : "Y", offset2, length2, fds == null ? "-" : "" + fds.FDs.Count);
			AssertValidBuffer (buffer1, offset1, length1);
			if (buffer2 == null) {
				if (length2 != 0)
					throw new ArgumentOutOfRangeException ("length2", "!= 0 while buffer2 == null");
				offset2 = 0;
			} else {
				AssertValidBuffer (buffer2, offset2, length2);
			}

			fixed (byte* ptr1 = buffer1, ptr2 = buffer2) {
				var iovecs = new Iovec[] {
					new Iovec {
						iov_base = (IntPtr) (ptr1 + offset1),
						iov_len = (ulong) length1,
					},
					new Iovec {
						iov_base = (IntPtr) (ptr2 + offset2),
						iov_len = (ulong) length2,
					},
				};
				/* Simulate short writes
				if (iovecs[0].iov_len == 0) {
					iovecs[1].iov_len = Math.Min (iovecs[1].iov_len, 5);
				} else {
					iovecs[0].iov_len = Math.Min (iovecs[0].iov_len, 5);
					iovecs[1].iov_len = 0;
				}
				*/
				byte[] cmsg = null;

				// Create copy of FDs to prevent the user from Dispose()ing the
				// FDs in another thread between writing the FDs into the cmsg
				// buffer and calling sendmsg()
				using (var fds2 = fds == null ? null : fds.Clone ()) {
					int fdCount = fds2 == null ? 0 : fds2.FDs.Count;
					if (fdCount != 0) {
						// Create one SCM_RIGHTS control message
						cmsg = new byte[Syscall.CMSG_SPACE ((uint) fdCount * sizeof (int))];
					}
					var msghdr = new Msghdr {
						msg_iov = iovecs,
						msg_iovlen = length2 == 0 ? 1 : 2,
						msg_control = cmsg,
						msg_controllen = cmsg == null ? 0 : cmsg.Length,
					};
					if (fdCount != 0) {
						var hdr = new Cmsghdr {
							cmsg_len = (long) Syscall.CMSG_LEN ((uint) fdCount * sizeof (int)),
							cmsg_level = UnixSocketProtocol.SOL_SOCKET,
							cmsg_type = UnixSocketControlMessage.SCM_RIGHTS,
						};
						hdr.WriteToBuffer (msghdr, 0);
						var dataOffset = Syscall.CMSG_DATA (msghdr, 0);
						fixed (byte* ptr = cmsg) {
							for (int i = 0; i < fdCount; i++)
								((int*) (ptr + dataOffset))[i] = fds2.FDs[i].Handle;
						}
					}
					long r;
					do {
						r = Syscall.sendmsg (fd, msghdr, MessageFlags.MSG_NOSIGNAL);
					} while (UnixMarshal.ShouldRetrySyscall ((int) r));
					if (r < 0)
						UnixMarshal.ThrowExceptionForLastError ();
					if (r == 0)
						throw new Exception ("sendmsg() returned 0");
					return r;
				}
			}
		}

		// Send the two buffers and the FDs using sendmsg(), handle short writes
		public unsafe void Sendmsg (byte[] buffer1, long offset1, long length1,
									byte[] buffer2, long offset2, long length2,
									DBus.Protocol.UnixFDArray fds)
		{
			//SendmsgShort (buffer1, offset1, length1, buffer2, offset2, length2, fds); return;
			long bytes_overall = (long) length1 + length2;
			long written = 0;
			while (written < bytes_overall) {
				if (written >= length1) {
					long written2 = written - length1;
					written += SendmsgShort (buffer2, offset2 + written2, length2 - written2, null, 0, 0, written == 0 ? fds : null);
				} else {
					written += SendmsgShort (buffer1, offset1 + written, length1 - written, buffer2, offset2, length2, written == 0 ? fds : null);
				}
			}
			if (written != bytes_overall)
				throw new Exception ("written != bytes_overall");
		}
	}
}

// vim: noexpandtab
// Local Variables:
// tab-width: 4
// c-basic-offset: 4
// indent-tabs-mode: t
// End:
