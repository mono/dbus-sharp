// Copyright 2007 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace DBus
{
	using Protocol;

	class TypeImplementer
	{
		public static readonly TypeImplementer Root = new TypeImplementer ("DBus.Proxies", false);
		AssemblyBuilder asmB;
		ModuleBuilder modB;
		static readonly object getImplLock = new Object ();

		Dictionary<Type,Type> map = new Dictionary<Type,Type> ();

		static MethodInfo getTypeFromHandleMethod = typeof (Type).GetMethod ("GetTypeFromHandle", new Type[] {typeof (RuntimeTypeHandle)});
		static ConstructorInfo argumentNullExceptionConstructor = typeof (ArgumentNullException).GetConstructor (new Type[] {typeof (string)});
		static ConstructorInfo messageWriterConstructor = typeof (MessageWriter).GetConstructor (Type.EmptyTypes);
		static MethodInfo messageWriterWriteArray = typeof (MessageWriter).GetMethod ("WriteArray");
		static MethodInfo messageWriterWriteDict = typeof (MessageWriter).GetMethod ("WriteFromDict");
		static MethodInfo messageWriterWriteStruct = typeof (MessageWriter).GetMethod ("WriteStructure");
		static MethodInfo messageReaderReadValue = typeof (MessageReader).GetMethod ("ReadValue", new Type[] { typeof (System.Type) });
		static MethodInfo messageReaderReadArray = typeof (MessageReader).GetMethod ("ReadArray", Type.EmptyTypes);
		static MethodInfo messageReaderReadDictionary = typeof (MessageReader).GetMethod ("ReadDictionary", Type.EmptyTypes);
		static MethodInfo messageReaderReadStruct = typeof (MessageReader).GetMethod ("ReadStruct", Type.EmptyTypes);
		static MethodInfo messageHelperGetDynamicValues = typeof (MessageHelper).GetMethod ("GetDynamicValues", new [] { typeof (Message) });

		static Dictionary<Type,MethodInfo> writeMethods = new Dictionary<Type,MethodInfo> ();
		static Dictionary<Type,object> typeWriters = new Dictionary<Type,object> ();

		static MethodInfo sendPropertyGetMethod = typeof (BusObject).GetMethod ("SendPropertyGet");
		static MethodInfo sendPropertySetMethod = typeof (BusObject).GetMethod ("SendPropertySet");
		static MethodInfo sendMethodCallMethod = typeof (BusObject).GetMethod ("SendMethodCall");
		static MethodInfo sendSignalMethod = typeof (BusObject).GetMethod ("SendSignal");
		static MethodInfo toggleSignalMethod = typeof (BusObject).GetMethod ("ToggleSignal");

		static Dictionary<EventInfo,DynamicMethod> hookup_methods = new Dictionary<EventInfo,DynamicMethod> ();
		static Dictionary<Type,MethodInfo> readMethods = new Dictionary<Type,MethodInfo> ();

		public TypeImplementer (string name, bool canSave)
		{
			asmB = AppDomain.CurrentDomain.DefineDynamicAssembly (new AssemblyName (name),
			                                                      canSave ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run);
			modB = asmB.DefineDynamicModule (name);
		}

		public Type GetImplementation (Type declType)
		{
			Type retT;

			lock (getImplLock)
				if (map.TryGetValue (declType, out retT))
					return retT;

			string proxyName = declType.FullName + "Proxy";

			Type parentType;

			if (declType.IsInterface)
				parentType = typeof (BusObject);
			else
				parentType = declType;

			TypeBuilder typeB = modB.DefineType (proxyName, TypeAttributes.Class | TypeAttributes.Public, parentType);

			string interfaceName = null;
			if (declType.IsInterface)
				Implement (typeB, declType, interfaceName = Mapper.GetInterfaceName (declType));

			foreach (Type iface in declType.GetInterfaces ())
				Implement (typeB, iface, interfaceName == null ? Mapper.GetInterfaceName (iface) : interfaceName);

			retT = typeB.CreateType ();

			lock (getImplLock)
				map[declType] = retT;

			return retT;
		}

		static void Implement (TypeBuilder typeB, Type iface, string interfaceName)
		{
			typeB.AddInterfaceImplementation (iface);

			HashSet<MethodInfo> evaluation_set = new HashSet<MethodInfo> (iface.GetMethods ());

			foreach (PropertyInfo declProp in iface.GetProperties ()) {
				GenHookupProperty (typeB, declProp, interfaceName, evaluation_set);
			}

			foreach (EventInfo declEvent in iface.GetEvents ()) {
				GenHookupEvent (typeB, declEvent, interfaceName, evaluation_set);
			}

			foreach (MethodInfo declMethod in evaluation_set) {
				MethodBuilder builder = CreateMethodBuilder (typeB, declMethod);
				ILGenerator ilg = builder.GetILGenerator ();
				GenHookupMethod (ilg, declMethod, sendMethodCallMethod, Mapper.GetInterfaceName (iface), declMethod.Name);
			}


		}

		public static MethodBuilder CreateMethodBuilder (TypeBuilder typeB, MethodInfo declMethod)
		{
			ParameterInfo[] parms = declMethod.GetParameters ();

			Type[] parmTypes = new Type[parms.Length];
			for (int i = 0 ; i < parms.Length ; i++)
				parmTypes[i] = parms[i].ParameterType;

			MethodAttributes attrs = declMethod.Attributes ^ MethodAttributes.Abstract;
			attrs ^= MethodAttributes.NewSlot;
			attrs |= MethodAttributes.Final;
			MethodBuilder method_builder = typeB.DefineMethod (declMethod.Name, attrs, declMethod.ReturnType, parmTypes);
			typeB.DefineMethodOverride (method_builder, declMethod);

			//define in/out/ref/name for each of the parameters
			for (int i = 0; i < parms.Length ; i++)
				method_builder.DefineParameter (i + 1, parms[i].Attributes, parms[i].Name);

			return method_builder;
		}

		public static DynamicMethod GetHookupMethod (EventInfo ei)
		{
			DynamicMethod hookupMethod;
			if (hookup_methods.TryGetValue (ei, out hookupMethod))
				return hookupMethod;

			if (ei.EventHandlerType.IsAssignableFrom (typeof (System.EventHandler)))
				Console.Error.WriteLine ("Warning: Cannot yet fully expose EventHandler and its subclasses: " + ei.EventHandlerType);

			MethodInfo declMethod = ei.EventHandlerType.GetMethod ("Invoke");

			hookupMethod = GetHookupMethod (declMethod, sendSignalMethod, Mapper.GetInterfaceName (ei), ei.Name);

			hookup_methods[ei] = hookupMethod;

			return hookupMethod;
		}

		public static DynamicMethod GetHookupMethod (MethodInfo declMethod, MethodInfo invokeMethod, string @interface, string member)
		{
			ParameterInfo[] delegateParms = declMethod.GetParameters ();
			Type[] hookupParms = new Type[delegateParms.Length+1];
			hookupParms[0] = typeof (BusObject);
			for (int i = 0; i < delegateParms.Length; ++i)
				hookupParms[i + 1] = delegateParms[i].ParameterType;

			DynamicMethod hookupMethod = new DynamicMethod ("Handle" + member, declMethod.ReturnType, hookupParms, typeof (MessageWriter));

			ILGenerator ilg = hookupMethod.GetILGenerator ();

			GenHookupMethod (ilg, declMethod, invokeMethod, @interface, member);

			return hookupMethod;
		}

		public static MethodInfo GetWriteMethod (Type t)
		{
			MethodInfo meth;

			if (writeMethods.TryGetValue (t, out meth))
				return meth;

			DynamicMethod method_builder = new DynamicMethod ("Write" + t.Name, typeof (void), new Type[] {typeof (MessageWriter), t}, typeof (MessageWriter), true);

			ILGenerator ilg = method_builder.GetILGenerator ();

			ilg.Emit (OpCodes.Ldarg_0);
			ilg.Emit (OpCodes.Ldarg_1);

			GenWriter (ilg, t);

			ilg.Emit (OpCodes.Ret);

			meth = method_builder;

			writeMethods[t] = meth;
			return meth;
		}

		public static TypeWriter<T> GetTypeWriter<T> ()
		{
			Type t = typeof (T);

			object value;
			if (typeWriters.TryGetValue (t, out value))
				return (TypeWriter<T>)value;

			MethodInfo mi = GetWriteMethod (t);
			DynamicMethod dm = mi as DynamicMethod;
			if (dm == null)
				return null;

			TypeWriter<T> tWriter = dm.CreateDelegate (typeof (TypeWriter<T>)) as TypeWriter<T>;
			typeWriters[t] = tWriter;
			return tWriter;
		}

		//takes the Writer instance and the value of Type t off the stack, writes it
		public static void GenWriter (ILGenerator ilg, Type t)
		{
			Type tUnder = t;

			if (t.IsEnum)
				tUnder = Enum.GetUnderlyingType (t);

			Type type = t;

			MethodInfo exactWriteMethod = typeof (MessageWriter).GetMethod ("Write", BindingFlags.ExactBinding | BindingFlags.Instance | BindingFlags.Public, null, new Type[] {tUnder}, null);

			if (exactWriteMethod != null) {
				ilg.Emit (OpCodes.Call, exactWriteMethod);
			} else if (t.IsArray) {
				exactWriteMethod = messageWriterWriteArray.MakeGenericMethod (type.GetElementType ());
				ilg.Emit (OpCodes.Call, exactWriteMethod);
			} else if (type.IsGenericType && (type.GetGenericTypeDefinition () == typeof (IDictionary<,>) || type.GetGenericTypeDefinition () == typeof (Dictionary<,>))) {
				Type[] genArgs = type.GetGenericArguments ();
				exactWriteMethod = messageWriterWriteDict.MakeGenericMethod (genArgs);
				ilg.Emit (OpCodes.Call, exactWriteMethod);
			} else {
				MethodInfo mi = messageWriterWriteStruct.MakeGenericMethod (t);
				ilg.Emit (OpCodes.Call, mi);
			}
		}

		public static IEnumerable<FieldInfo> GetMarshalFields (Type type)
		{
			// FIXME: Field order!
			return type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		}

		private static void EmitThis (ILGenerator ilg)
		{
			//the 'this' instance
			ilg.Emit (OpCodes.Ldarg_0);

			ilg.Emit (OpCodes.Castclass, typeof (BusObject));

		}

		public static void GenHookupProperty (TypeBuilder typeB, PropertyInfo declProp, string @interface, HashSet<MethodInfo> evaluating)
		{
			Type[] indexers = declProp.GetIndexParameters ().Select (x => x.ParameterType).ToArray ();
			PropertyBuilder prop_builder = typeB.DefineProperty (declProp.Name, 
									     declProp.Attributes, 
									     declProp.PropertyType, 
									     indexers);

			MethodInfo[] sources = new MethodInfo[] { declProp.GetGetMethod (),
								  declProp.GetSetMethod () };

			foreach (MethodInfo source in sources)
			{
				if (null == source)
					continue;

				evaluating.Remove (source);

				MethodBuilder meth_builder = CreateMethodBuilder (typeB, source);
				ILGenerator ilg = meth_builder.GetILGenerator ();

				bool isGet = typeof(void) != source.ReturnType;

				MethodInfo target = isGet ? sendPropertyGetMethod : sendPropertySetMethod;

				EmitThis (ilg);

				ilg.Emit (OpCodes.Ldstr, @interface);
				ilg.Emit (OpCodes.Ldstr, declProp.Name);

				if (!isGet)
				{
					ilg.Emit (OpCodes.Ldarg_1);
					ilg.Emit (OpCodes.Box, source.GetParameters ()[0].ParameterType);
				}

				ilg.Emit (OpCodes.Tailcall);
				ilg.Emit (target.IsFinal ? OpCodes.Call : OpCodes.Callvirt, target);

				if (isGet)
					ilg.Emit (source.ReturnType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, source.ReturnType);

				ilg.Emit (OpCodes.Ret);

				if (isGet)
					prop_builder.SetGetMethod (meth_builder);
				else
					prop_builder.SetSetMethod (meth_builder);
			}
		}

		public static void GenHookupEvent (TypeBuilder typeB, EventInfo declEvent, string @interface, HashSet<MethodInfo> evaluating)
		{
			EventBuilder event_builder = typeB.DefineEvent (declEvent.Name, 
									declEvent.Attributes, 
									declEvent.EventHandlerType);

			MethodInfo[] sources = new MethodInfo[] { declEvent.GetAddMethod (),
								  declEvent.GetRemoveMethod () };

			foreach (MethodInfo source in sources)
			{
				if (null == source)
					continue;

				evaluating.Remove (source);

				MethodBuilder meth_builder = CreateMethodBuilder (typeB, source);
				ILGenerator ilg = meth_builder.GetILGenerator ();

				bool adding = sources[0] == source;

				EmitThis (ilg);

				//interface
				ilg.Emit (OpCodes.Ldstr, @interface);

				ilg.Emit (OpCodes.Ldstr, declEvent.Name);

				ilg.Emit (OpCodes.Ldarg_1);

				ilg.Emit (OpCodes.Ldc_I4, adding ? 1 : 0);

				ilg.Emit (OpCodes.Tailcall);
				ilg.Emit (toggleSignalMethod.IsFinal ? OpCodes.Call : OpCodes.Callvirt, toggleSignalMethod);
				ilg.Emit (OpCodes.Ret);

				if (adding)
					event_builder.SetAddOnMethod (meth_builder);
				else
					event_builder.SetRemoveOnMethod (meth_builder);
			}
		}

		public static void GenHookupMethod (ILGenerator ilg, MethodInfo declMethod, MethodInfo invokeMethod, string @interface, string member)
		{
			ParameterInfo[] parms = declMethod.GetParameters ();
			Type retType = declMethod.ReturnType;

			EmitThis (ilg);

			//interface
			ilg.Emit (OpCodes.Ldstr, @interface);

			//member
			ilg.Emit (OpCodes.Ldstr, member);

			//signature
			Signature inSig;
			Signature outSig;
			SigsForMethod (declMethod, out inSig, out outSig);

			ilg.Emit (OpCodes.Ldstr, inSig.Value);

			LocalBuilder writer = ilg.DeclareLocal (typeof (MessageWriter));
			ilg.Emit (OpCodes.Newobj, messageWriterConstructor);
			ilg.Emit (OpCodes.Stloc, writer);

			foreach (ParameterInfo parm in parms)
			{
				if (parm.IsOut)
					continue;

				Type t = parm.ParameterType;
				//offset by one to account for "this"
				int i = parm.Position + 1;

				//null checking of parameters (but not their recursive contents)
				if (!t.IsValueType) {
					Label notNull = ilg.DefineLabel ();

					//if the value is null...
					ilg.Emit (OpCodes.Ldarg, i);
					ilg.Emit (OpCodes.Brtrue_S, notNull);

					//...throw Exception
					string paramName = parm.Name;
					ilg.Emit (OpCodes.Ldstr, paramName);
					ilg.Emit (OpCodes.Newobj, argumentNullExceptionConstructor);
					ilg.Emit (OpCodes.Throw);

					//was not null, so all is well
					ilg.MarkLabel (notNull);
				}

				ilg.Emit (OpCodes.Ldloc, writer);

				//the parameter
				ilg.Emit (OpCodes.Ldarg, i);

				GenWriter (ilg, t);
			}

			ilg.Emit (OpCodes.Ldloc, writer);

			//the expected return Type
			GenTypeOf (ilg, retType);

			LocalBuilder exc = ilg.DeclareLocal (typeof (Exception));
			ilg.Emit (OpCodes.Ldloca_S, exc);

			//make the call
			ilg.Emit (OpCodes.Callvirt, invokeMethod);

			//define a label we'll use to deal with a non-null Exception
			Label noErr = ilg.DefineLabel ();

			//if the out Exception is not null...
			ilg.Emit (OpCodes.Ldloc, exc);
			ilg.Emit (OpCodes.Brfalse_S, noErr);

			//...throw it.
			ilg.Emit (OpCodes.Ldloc, exc);
			ilg.Emit (OpCodes.Throw);

			//Exception was null, so all is well
			ilg.MarkLabel (noErr);

			if (invokeMethod.ReturnType == typeof (MessageReader)) {
				LocalBuilder reader = ilg.DeclareLocal (typeof (MessageReader));
				ilg.Emit (OpCodes.Stloc, reader);

				foreach (ParameterInfo parm in parms)
				{
					//t.IsByRef
					if (!parm.IsOut)
						continue;

					Type t = parm.ParameterType.GetElementType ();
					//offset by one to account for "this"
					int i = parm.Position + 1;

					ilg.Emit (OpCodes.Ldarg, i);
					ilg.Emit (OpCodes.Ldloc, reader);
					GenReader (ilg, t);
					ilg.Emit (OpCodes.Stobj, t);
				}

				if (retType != typeof (void)) {
					ilg.Emit (OpCodes.Ldloc, reader);
					GenReader (ilg, retType);
				}

				ilg.Emit (OpCodes.Ret);
				return;
			}

			if (retType == typeof (void)) {
				//we aren't expecting a return value, so throw away the (hopefully) null return
				if (invokeMethod.ReturnType != typeof (void))
					ilg.Emit (OpCodes.Pop);
			} else {
				if (retType.IsValueType)
					ilg.Emit (OpCodes.Unbox_Any, retType);
				else
					ilg.Emit (OpCodes.Castclass, retType);
			}

			ilg.Emit (OpCodes.Ret);
		}


		public static bool SigsForMethod (MethodInfo mi, out Signature inSig, out Signature outSig)
		{
			inSig = Signature.Empty;
			outSig = Signature.Empty;

			foreach (ParameterInfo parm in mi.GetParameters ()) {
				if (parm.IsOut)
					outSig += Signature.GetSig (parm.ParameterType.GetElementType ());
				else
					inSig += Signature.GetSig (parm.ParameterType);
			}

			outSig += Signature.GetSig (mi.ReturnType);

			return true;
		}

		static void InitReaders ()
		{
			foreach (MethodInfo mi in typeof (MessageReader).GetMethods (BindingFlags.Instance | BindingFlags.Public)) {
				if (!mi.Name.StartsWith ("Read"))
					continue;
				if (mi.ReturnType == typeof (void))
					continue;
				if (mi.GetParameters ().Length != 0)
					continue;

				readMethods[mi.ReturnType] = mi;
			}
		}

		internal static MethodInfo GetReadMethod (Type t)
		{
			if (readMethods.Count == 0)
				InitReaders ();

			MethodInfo mi;
			if (readMethods.TryGetValue (t, out mi))
				return mi;

			return null;
		}

		internal static MethodCall GenMethodCall (MethodInfo target)
		{
			Signature inSig, outSig;
			SigsForMethod (target, out inSig, out outSig);
			return new MethodCall {
				Out = outSig,
				In = inSig,
				Call = GenCaller (target),
				MetaData = target
			};
		}

		internal static PropertyCall GenPropertyCall (PropertyInfo target)
		{
			var pc = new PropertyCall {
				Get = GenGetCall (target),
				Set = GenSetCall (target),
				MetaData = target
			};

			return pc;
		}

		internal static MethodCaller GenGetCall (PropertyInfo target)
		{
			var mi = target.GetGetMethod ();

			if (null == mi) {
				return null;
			}

			var parms = new Type[] {
				typeof (object),
				typeof (MessageReader),
				typeof (Message),
				typeof (MessageWriter)
			};
			var method = new DynamicMethod ("PropertyGet", typeof(void), parms, typeof(MessageReader));

			var ilg = method.GetILGenerator ();

			var retLocal = ilg.DeclareLocal (mi.ReturnType);

			ilg.Emit (OpCodes.Ldarg_0);
			ilg.EmitCall (mi.IsFinal ? OpCodes.Call : OpCodes.Callvirt, mi, null);
			ilg.Emit (OpCodes.Stloc, retLocal);

			ilg.Emit (OpCodes.Ldarg_3);
			ilg.Emit (OpCodes.Ldloc, retLocal);
			GenWriter (ilg, mi.ReturnType);

			ilg.Emit (OpCodes.Ret);

			return (MethodCaller) method.CreateDelegate (typeof(MethodCaller));
		}

		internal static MethodCaller GenSetCall (PropertyInfo target)
		{
			var mi = target.GetSetMethod ();

			if (null == mi) {
				return null;
			}

			var parms = new Type[] {
				typeof (object),
				typeof (MessageReader),
				typeof (Message),
				typeof (MessageWriter)
			};
			var method = new DynamicMethod ("PropertySet", typeof(void), parms, typeof(MessageReader));

			var ilg = method.GetILGenerator ();

			if (null == messageHelperGetDynamicValues) {
				throw new MissingMethodException (typeof(MessageHelper).Name, "GetDynamicValues");
			}

			var args = ilg.DeclareLocal (typeof(object[]));
			var arg = ilg.DeclareLocal (typeof(object));
			var v = ilg.DeclareLocal (target.PropertyType);

			ilg.Emit (OpCodes.Ldarg_2);
			ilg.Emit (OpCodes.Call, messageHelperGetDynamicValues);
			ilg.Emit (OpCodes.Stloc, args);

			ilg.Emit (OpCodes.Ldloc, args);
			ilg.Emit (OpCodes.Ldc_I4_2);
			ilg.Emit (OpCodes.Ldelem, typeof(object));
			ilg.Emit (OpCodes.Stloc, arg);

			var cast = target.PropertyType.IsValueType
				? OpCodes.Unbox_Any
				: OpCodes.Castclass;

			ilg.Emit (OpCodes.Ldloc, arg);
			ilg.Emit (cast, target.PropertyType);
			ilg.Emit (OpCodes.Stloc, v);

			ilg.Emit (OpCodes.Ldarg_0);
			ilg.Emit (OpCodes.Ldloc, v);
			ilg.Emit (mi.IsFinal ? OpCodes.Call : OpCodes.Callvirt, mi);

			ilg.Emit (OpCodes.Ret);

			return (MethodCaller) method.CreateDelegate (typeof(MethodCaller));
		}

		internal static MethodCaller GenGetAllCall (Type @interface)
		{
			var parms = new Type[] {
				typeof (object),
				typeof (MessageReader),
				typeof (Message),
				typeof (MessageWriter)
			};
			var method = new DynamicMethod ("PropertyGetAll", typeof(void), parms, typeof(MessageReader));

			var ilg = method.GetILGenerator ();
			var dctT = typeof(Dictionary<string, object>);

			var strObj = new [] { typeof(string), typeof(object) };
			var dctConstructor = dctT.GetConstructor (new Type[0]);
			var dctAdd = dctT.GetMethod ("Add", strObj);

			var accessors = @interface.GetProperties ().Where (x => null != x.GetGetMethod());

			var dct = ilg.DeclareLocal (dctT);
			var val = ilg.DeclareLocal (typeof(object));

			ilg.Emit (OpCodes.Newobj, dctConstructor);
			ilg.Emit (OpCodes.Stloc, dct);
			foreach (var property in accessors) {
				var mi = property.GetGetMethod ();

				ilg.Emit (OpCodes.Ldarg_0);
				ilg.Emit (mi.IsFinal ? OpCodes.Call : OpCodes.Callvirt, mi);
				if (mi.ReturnType.IsValueType) {
					ilg.Emit (OpCodes.Box, mi.ReturnType);
				}
				// TODO: Cast object references to typeof(object)?
				ilg.Emit (OpCodes.Stloc, val);

				ilg.Emit (OpCodes.Ldloc, dct);
				ilg.Emit (OpCodes.Ldstr, property.Name);
				ilg.Emit (OpCodes.Ldloc, val);
				ilg.Emit (OpCodes.Call, dctAdd);
			}
			ilg.Emit (OpCodes.Ldarg_3);
			ilg.Emit (OpCodes.Ldloc, dct);
			GenWriter (ilg, dctT);

			ilg.Emit (OpCodes.Ret);

			return (MethodCaller) method.CreateDelegate (typeof(MethodCaller));
		}

		internal static MethodCaller GenCaller (MethodInfo target)
		{
			DynamicMethod hookupMethod = GenReadMethod (target);
			MethodCaller caller = hookupMethod.CreateDelegate (typeof (MethodCaller)) as MethodCaller;
			return caller;
		}

		internal static DynamicMethod GenReadMethod (MethodInfo target)
		{
			Type[] parms = new Type[] { typeof (object), typeof (MessageReader), typeof (Message), typeof (MessageWriter) };
			DynamicMethod hookupMethod = new DynamicMethod ("Caller", typeof (void), parms, typeof (MessageReader));
			Gen (hookupMethod, target);
			return hookupMethod;
		}

		static void Gen (DynamicMethod hookupMethod, MethodInfo declMethod)
		{
			ILGenerator ilg = hookupMethod.GetILGenerator ();

			ParameterInfo[] parms = declMethod.GetParameters ();
			Type retType = declMethod.ReturnType;

			// The target instance
			ilg.Emit (OpCodes.Ldarg_0);

			Dictionary<ParameterInfo,LocalBuilder> locals = new Dictionary<ParameterInfo,LocalBuilder> ();

			foreach (ParameterInfo parm in parms) {

				Type parmType = parm.ParameterType;

				if (parm.IsOut) {
					LocalBuilder parmLocal = ilg.DeclareLocal (parmType.GetElementType ());
					locals[parm] = parmLocal;
					ilg.Emit (OpCodes.Ldloca, parmLocal);
					continue;
				}

				ilg.Emit (OpCodes.Ldarg_1);
				GenReader (ilg, parmType);
			}

			ilg.Emit (declMethod.IsFinal ? OpCodes.Call : OpCodes.Callvirt, declMethod);

			foreach (ParameterInfo parm in parms) {
				if (!parm.IsOut)
					continue;

				Type parmType = parm.ParameterType.GetElementType ();

				LocalBuilder parmLocal = locals[parm];
				ilg.Emit (OpCodes.Ldarg_3); // writer
				ilg.Emit (OpCodes.Ldloc, parmLocal);
				GenWriter (ilg, parmType);
			}

			if (retType != typeof (void)) {
				// Skip reply message construction if MessageWriter is null

				LocalBuilder retLocal = ilg.DeclareLocal (retType);
				ilg.Emit (OpCodes.Stloc, retLocal);

				ilg.Emit (OpCodes.Ldarg_3); // writer
				ilg.Emit (OpCodes.Ldloc, retLocal);
				GenWriter (ilg, retType);
			}

			ilg.Emit (OpCodes.Ret);
		}

		public static void GenReader (ILGenerator ilg, Type t)
		{
			Type tUnder = t;

			if (t.IsEnum)
				tUnder = Enum.GetUnderlyingType (t);

			Type gDef = t.IsGenericType ? t.GetGenericTypeDefinition () : null;

			MethodInfo exactMethod = GetReadMethod (tUnder);
			if (exactMethod != null) {
				ilg.Emit (OpCodes.Callvirt, exactMethod);
			} else if (t.IsArray) {
				var tarray = t.GetElementType ();
				ilg.Emit (OpCodes.Call, messageReaderReadArray.MakeGenericMethod (new[] { tarray }));
			} else if (gDef != null && (gDef == typeof (IDictionary<,>) || gDef == typeof (Dictionary<,>))) {
				var tmpTypes = t.GetGenericArguments ();
				MethodInfo mi = messageReaderReadDictionary.MakeGenericMethod (new[] { tmpTypes[0], tmpTypes[1] });
				ilg.Emit (OpCodes.Callvirt, mi);
			} else if (t.IsInterface)
				GenFallbackReader (ilg, tUnder);
			else if (!tUnder.IsValueType) {
				ilg.Emit (OpCodes.Callvirt, messageReaderReadStruct.MakeGenericMethod (tUnder));
			} else
				GenFallbackReader (ilg, tUnder);
		}

		public static void GenFallbackReader (ILGenerator ilg, Type t)
		{
			// TODO: do we want non-tUnder here for Castclass use?
			if (ProtocolInformation.Verbose)
				Console.Error.WriteLine ("Bad! Generating fallback reader for " + t);

			// The Type parameter
			GenTypeOf (ilg, t);
			ilg.Emit (OpCodes.Callvirt, messageReaderReadValue);

			if (t.IsValueType)
				ilg.Emit (OpCodes.Unbox_Any, t);
			else
				ilg.Emit (OpCodes.Castclass, t);
		}

		static void GenTypeOf (ILGenerator ilg, Type t)
		{
			ilg.Emit (OpCodes.Ldtoken, t);
			ilg.Emit (OpCodes.Call, getTypeFromHandleMethod);
		}
	}

	internal static class MethodBaseExtensions {

		static IDictionary<Type, HashSet<MethodBase>> events = new Dictionary<Type, HashSet<MethodBase>>();
		static IDictionary<Type, HashSet<MethodBase>> properties = new Dictionary<Type, HashSet<MethodBase>>();

		private static void InitialiseType (Type type)
		{
			lock (typeof(MethodBaseExtensions)) {
				if (events.ContainsKey (type) && properties.ContainsKey (type))
					return;

				events [type]     = new HashSet<MethodBase>();
				properties [type] = new HashSet<MethodBase>();

				type.GetEvents ().Aggregate (events [type], (set, evt) => {
					set.Add (evt.GetAddMethod ());
					set.Add (evt.GetRemoveMethod ());
					return set;
				});
				type.GetProperties ().Aggregate (properties [type], (set, prop) => {
					set.Add (prop.GetGetMethod ());
					set.Add (prop.GetSetMethod ());
					return set;
				});

				events [type].Remove (null);
				properties [type].Remove (null);
			}
		}

		public static bool IsEvent (this MethodBase method)
		{
			InitialiseType (method.DeclaringType);
			HashSet<MethodBase> methods = events [method.DeclaringType];
			return methods.Contains (method);
		}

		public static bool IsProperty (this MethodBase method)
		{
			InitialiseType (method.DeclaringType);
			HashSet<MethodBase> methods = properties [method.DeclaringType];
			return methods.Contains (method);
		}
	}

	internal delegate void TypeWriter<T> (MessageWriter writer, T value);

	internal delegate void MethodCaller (object instance, MessageReader rdr, Message msg, MessageWriter ret);
}
