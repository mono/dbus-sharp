// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using org.freedesktop.DBus;

namespace DBus
{
	using Protocol;

	internal class PropertyCallers {
		public Type Type { get; set; }
		public MethodCaller Get { get; set; }
		public MethodCaller Set { get; set; }
	}

	//TODO: perhaps ExportObject should not derive from BusObject
	internal class ExportObject : BusObject, IDisposable
	{
		//maybe add checks to make sure this is not called more than once
		//it's a bit silly as a property
		bool isRegistered = false;

		Dictionary<string, PropertyInfo> propertyInfoCache = new Dictionary<string, PropertyInfo> ();

		Dictionary<string, MethodInfo> methodInfoCache = new Dictionary<string, MethodInfo> ();

		static readonly Dictionary<MethodInfo, MethodCaller> mCallers = new Dictionary<MethodInfo, MethodCaller> ();

		static readonly Dictionary<PropertyInfo, PropertyCallers> pCallers = new Dictionary<PropertyInfo, PropertyCallers> ();

		public ExportObject (Connection conn, ObjectPath object_path, object obj) : base (conn, null, object_path)
		{
			Object = obj;
		}

		public virtual bool Registered
		{
			get {
				return isRegistered;
			}
			set {
				if (value == isRegistered)
					return;

				Type type = Object.GetType ();

				foreach (var memberForType in Mapper.GetPublicMembers (type)) {
					MemberInfo mi = memberForType.Value;
					EventInfo ei = mi as EventInfo;

					if (ei == null)
						continue;

					Delegate dlg = GetHookupDelegate (ei);

					if (value)
						ei.AddEventHandler (Object, dlg);
					else
						ei.RemoveEventHandler (Object, dlg);
				}

				isRegistered = value;
			}
		}

		internal virtual void WriteIntrospect (Introspector intro)
		{
			intro.WriteType (Object.GetType ());
		}

		internal static MethodCaller GetMCaller (MethodInfo mi)
		{
			MethodCaller mCaller;
			if (!mCallers.TryGetValue (mi, out mCaller)) {
				mCaller = TypeImplementer.GenCaller (mi);
				mCallers[mi] = mCaller;
			}
			return mCaller;
		}

		public static ExportObject CreateExportObject (Connection conn, ObjectPath object_path, object obj)
		{
			return new ExportObject (conn, object_path, obj);
		}

		private static string Key (string iface, string member)
		{
			return string.Format ("{0}.{1}", iface, member);
		}

		public virtual void HandleMethodCall (MessageContainer method_call)
		{
			switch (method_call.Interface) {
			case "org.freedesktop.DBus.Properties":
				HandlePropertyCall (method_call);
				return;
			}

			var cache_key = Key (method_call.Interface, method_call.Member);
			MethodInfo mi;
			if (!methodInfoCache.TryGetValue (cache_key, out mi)) {
				mi = Mapper.GetMethod (Object.GetType (), method_call);
				methodInfoCache [cache_key] = mi;
			}

			if (mi == null) {
				conn.MaybeSendUnknownMethodError (method_call);
				return;
			}

			MethodCaller mCaller;
			if (!mCallers.TryGetValue (mi, out mCaller)) {
				mCaller = TypeImplementer.GenCaller (mi);
				mCallers[mi] = mCaller;
			}

			Signature inSig, outSig;
			TypeImplementer.SigsForMethod (mi, out inSig, out outSig);

			Message msg = method_call.Message;
			MessageReader msgReader = new MessageReader (msg);
			MessageWriter retWriter = new MessageWriter ();

			Exception raisedException = null;
			try {
				mCaller (Object, msgReader, msg, retWriter);
			} catch (Exception e) {
				raisedException = e;
			}

			IssueReply (method_call, outSig, retWriter, mi, raisedException);
		}

		private void IssueReply (MessageContainer method_call, Signature outSig, MessageWriter retWriter, MethodInfo mi, Exception raisedException)
		{
			Message msg = method_call.Message;

			if (!msg.ReplyExpected)
				return;

			Message replyMsg;

			if (raisedException == null) {
				MessageContainer method_return = new MessageContainer {
					Type = MessageType.MethodReturn,
					Destination = method_call.Sender,
					ReplySerial = msg.Header.Serial
				};
				replyMsg = method_return.Message;
				replyMsg.AttachBodyTo (retWriter);
				replyMsg.Signature = outSig;
			} else {
				// BusException allows precisely formatted Error messages.
				BusException busException = raisedException as BusException;
				if (busException != null)
					replyMsg = method_call.CreateError (busException.ErrorName, busException.ErrorMessage);
				else if (raisedException is ArgumentException && raisedException.TargetSite.Name == mi.Name) {
					// Name match trick above is a hack since we don't have the resolved MethodInfo.
					ArgumentException argException = (ArgumentException)raisedException;
					using (System.IO.StringReader sr = new System.IO.StringReader (argException.Message)) {
						replyMsg = method_call.CreateError ("org.freedesktop.DBus.Error.InvalidArgs", sr.ReadLine ());
					}
				} else
					replyMsg = method_call.CreateError (Mapper.GetInterfaceName (raisedException.GetType ()), raisedException.Message);
			}

			conn.Send (replyMsg);
		}

		private void HandlePropertyCall (MessageContainer method_call)
		{
			Message msg = method_call.Message;
			MessageReader msgReader = new MessageReader (msg);
			MessageWriter retWriter = new MessageWriter ();

			object[] args = MessageHelper.GetDynamicValues (msg);

			string face = (string) args [0];
			string name = (string) args [1];

			PropertyInfo p = GetPropertyInfo (Object.GetType (), face, name);
			PropertyCallers pcs = GetPropertyCallers (p);

			MethodCaller pc;
			MethodInfo mi;

			switch (method_call.Member) {
			case "Set":
				pc = pcs.Set;
				mi = p.GetSetMethod ();
				break;
			case "Get":
				pc = pcs.Get;
				mi = p.GetGetMethod ();
				break;
			case "GetAll":
				throw new NotImplementedException ();
			default:
				throw new ArgumentException (string.Format ("No such method {0}.{1}", method_call.Interface, method_call.Member));
			}

			if (null == pc || null == mi) {
				throw new MissingMethodException ();
			}

			Exception raised = null;
			try {
				pc (Object, msgReader, msg, retWriter);
			} catch (Exception e) {
				raised = e;
			}

			Signature inSig, outSig;
			TypeImplementer.SigsForMethod (mi, out inSig, out outSig);

			IssueReply (method_call, outSig, retWriter, mi, raised);
		}

		private PropertyInfo GetPropertyInfo (Type type, string @interface, string property)
		{
			var key = Key (@interface, property);
			PropertyInfo pi;
			if (!propertyInfoCache.TryGetValue (key, out pi)) {
				pi = Mapper.GetPublicProperties(Mapper.GetInterfaceType (type, @interface)).First (x => property == x.Name);
				propertyInfoCache [key] = pi;
			}

			return  pi;
		}

		private static PropertyCallers GetPropertyCallers (PropertyInfo pi)
		{
			PropertyCallers pCaller;
			if (!pCallers.TryGetValue (pi, out pCaller)) {
				pCaller = TypeImplementer.GenPropertyCallers (pi);
				pCallers[pi] = pCaller;
			}

			return pCaller;
		}

		public object Object {
			get;
			private set;
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		~ExportObject ()
		{
			Dispose (false);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposing) {
				if (Object != null) {
					Registered = false;
					Object = null;
				}
			}
		}
	}
}
