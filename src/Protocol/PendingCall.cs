// Copyright 2007 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Threading;

namespace DBus.Protocol
{
	public class PendingCall : IAsyncResult, IDisposable
	{
		private Connection conn;
		private Message reply;
		private ManualResetEvent waitHandle = new ManualResetEvent (false);
		private bool completedSync = false;
		private bool keepFDs;
		private CancellationTokenSource stopWait = new CancellationTokenSource();

		public event Action<Message> Completed;

		public PendingCall(Connection conn)
			: this (conn, false)
		{
		}

		public PendingCall (Connection conn, bool keepFDs)
		{
			this.conn = conn;
			this.keepFDs = keepFDs;
		}

		public void Dispose()
		{
			stopWait.Dispose ();
		}

		internal bool KeepFDs
		{
			get {
				return keepFDs;
			}
		}

		public Message Reply {
			get {
				while (reply == null) {
					try {
						conn.Iterate (stopWait.Token);
					}
					catch (OperationCanceledException) {
					}
				}
				return reply;
			}

			set {
				if (reply != null)
					throw new Exception ("Cannot handle reply more than once");
				reply = value;

				waitHandle.Set ();
				
				stopWait.Cancel ();

				if (Completed != null)
					Completed (reply);
			}
		}

		public void Cancel ()
		{
			throw new NotImplementedException ();
		}

		#region IAsyncResult Members

		object IAsyncResult.AsyncState {
			get {
				return conn;
			}
		}

		WaitHandle IAsyncResult.AsyncWaitHandle {
			get {
				return waitHandle;
			}
		}

		bool IAsyncResult.CompletedSynchronously {
			get {
				return reply != null && completedSync;
			}
		}

		bool IAsyncResult.IsCompleted {
			get {
				return reply != null;
			}
		}

		#endregion
	}
}
