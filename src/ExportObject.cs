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

	internal class PropertyCall {
		public MethodCaller Get { get; set; }
		public MethodCaller Set { get; set; }
		public PropertyInfo MetaData { get; set; }
	}

	internal class MethodCall {
		public Signature Out { get; set; }
		public Signature In { get; set; }
		public MethodCaller Call { get; set; }
		public MethodInfo MetaData { get; set; }
	}

	internal class MethodDictionary : Dictionary<string, MethodCall> { }
	internal class PropertyDictionary : Dictionary<string, PropertyCall> {
		public MethodCaller All { get; set; }
	}

	internal class InterfaceMethods : Dictionary<string, MethodDictionary> { }
	internal class InterfaceProperties : Dictionary<string, PropertyDictionary> { }

	internal class DBusMemberTable {

		public Type ObjectType { get; private set; }

		public DBusMemberTable(Type type)
		{
			ObjectType = type;
		}

		InterfaceMethods Methods = new InterfaceMethods();
		InterfaceProperties Properties = new InterfaceProperties();

		public MethodCall GetMethodCall (string iface, string name)
		{
			return Lookup<InterfaceMethods, MethodDictionary, string, string, MethodCall> (
				Methods,
				iface,
				name,
				(i, n) => {
					Type it = Mapper.GetInterfaceType (ObjectType, i);
					MethodInfo mi = it.GetMethod (n);
					return TypeImplementer.GenMethodCall (mi);
				}
			);
		}

		public MethodCaller GetPropertyAllCall (string iface)
		{
			PropertyDictionary calls;
			if (!Properties.TryGetValue(iface, out calls)) {
				Properties [iface] = calls = new PropertyDictionary ();
			}

			if (null == calls.All) {
				Type it = Mapper.GetInterfaceType (ObjectType, iface);
				calls.All = TypeImplementer.GenGetAllCall (it);
			}

			return calls.All;
		}

		public PropertyCall GetPropertyCall (string iface, string name)
		{
			return Lookup<InterfaceProperties, PropertyDictionary, string, string, PropertyCall> (
				Properties,
				iface,
				name,
				(i, n) => {
					Type it = Mapper.GetInterfaceType(ObjectType, i);
					PropertyInfo pi = it.GetProperty(n);
					return TypeImplementer.GenPropertyCall (pi);
				}
			);
		}

		private static V Lookup<TMap1,TMap2,A,B,V> (TMap1 map, A k1, B k2, Func<A,B,V> factory)
			where TMap2 : IDictionary<B, V>, new()
			where TMap1 : IDictionary<A, TMap2>
		{
			TMap2 first;
			if (!map.TryGetValue (k1, out first)) {
				map [k1] = first = new TMap2 ();
			}

			V value;
			if (!first.TryGetValue (k2, out value)) {
				first [k2] = value = factory (k1, k2);
			}

			return value;
		}

	}

	//TODO: perhaps ExportObject should not derive from BusObject
	internal class ExportObject : BusObject, IDisposable
	{
		//maybe add checks to make sure this is not called more than once
		//it's a bit silly as a property
		bool isRegistered = false;

		static readonly Dictionary<Type, DBusMemberTable> typeMembers = new Dictionary<Type, DBusMemberTable>();

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

		public static ExportObject CreateExportObject (Connection conn, ObjectPath object_path, object obj)
		{
			Type type = obj.GetType ();
			DBusMemberTable table;
			if (!typeMembers.TryGetValue (type, out table)) {
				typeMembers [type] = new DBusMemberTable (type);
			}
			return new ExportObject (conn, object_path, obj);
		}

		public virtual void HandleMethodCall (MessageContainer method_call)
		{
			switch (method_call.Interface) {
			case "org.freedesktop.DBus.Properties":
				HandlePropertyCall (method_call);
				return;
			}

			MethodCall mCaller = null;

			try {
				mCaller = typeMembers[Object.GetType()].GetMethodCall(
					method_call.Interface,
					method_call.Member
				);
			}
			catch { /* No Such Member */ }

			if (mCaller == null) {
				conn.MaybeSendUnknownMethodError (method_call);
				return;
			}

			Signature inSig  = mCaller.In,
			          outSig = mCaller.Out;

			Message msg = method_call.Message;
			MessageReader msgReader = new MessageReader (msg);
			MessageWriter retWriter = new MessageWriter ();

			Exception raisedException = null;
			try {
				mCaller.Call (Object, msgReader, msg, retWriter);
			} catch (Exception e) {
				raisedException = e;
			}

			IssueReply (method_call, outSig, retWriter, mCaller.MetaData, raisedException);
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

			if ("GetAll" == method_call.Member) {
				Signature asv = Signature.MakeDict (Signature.StringSig, Signature.VariantSig);

				MethodCaller call = typeMembers [Object.GetType ()].GetPropertyAllCall (face);

				Exception ex = null;
				try {
					call (Object, msgReader, msg, retWriter);
				} catch (Exception e) { ex = e; }

				IssueReply (method_call, asv, retWriter, null, ex);
				return;
			}

			string name = (string) args [1];

			PropertyCall pcs = typeMembers[Object.GetType()].GetPropertyCall (
				face,
				name
			);

			MethodInfo mi;
			MethodCaller pc;
			Signature outSig, inSig = method_call.Signature;

			switch (method_call.Member) {
			case "Set":
				mi = pcs.MetaData.GetSetMethod ();
				pc = pcs.Set;
				outSig = Signature.Empty;
				break;
			case "Get":
				mi = pcs.MetaData.GetGetMethod ();
				pc = pcs.Get;
				outSig = Signature.GetSig(mi.ReturnType);
				break;
			default:
				throw new ArgumentException (string.Format ("No such method {0}.{1}", method_call.Interface, method_call.Member));
			}

			if (null == pc) {
				conn.MaybeSendUnknownMethodError (method_call);
				return;
			}

			Exception raised = null;
			try {
				pc (Object, msgReader, msg, retWriter);
			} catch (Exception e) {
				raised = e;
			}

			IssueReply (method_call, outSig, retWriter, mi, raised);
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
