using System;
using System.Xml.Linq;
using System.Collections.Generic;

using DBus;
using DBus.Protocol;

using NUnit;
using NUnit.Framework;

namespace DBus.Tests
{
	[TestFixture]
	public class IntrospectorTest
	{
		Introspector intro;

		[SetUp]
		public void Setup ()
		{
			intro = new Introspector ();
		}

		[Interface ("org.dbussharp.IntrospectableTest")]
		interface IObjectIntrospected
		{
			string Method1 (int foo);
			[Obsolete]
			void Method2 (out long value);
			Dictionary<int, string[]> Dict { get; }
		}

		public class ObjectIntrospectedImpl : IObjectIntrospected
		{
			public string Method1 (int foo)
			{
				return string.Empty;
			}

			public void Method2 (out long value)
			{
				value = 0;
			}

			public Dictionary<int, string[]> Dict {
				get { return null; }
			}
		}

		const string expectedOutputSimpleInterface = @"<!DOCTYPE node PUBLIC ""-//freedesktop//DTD D-BUS Object Introspection 1.0//EN"" ""http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd"">
			<!-- dbus-sharp 0.7.0 -->
				<node>
				<interface name=""org.freedesktop.DBus.Introspectable"">
				<method name=""Introspect"">
				<arg name=""data"" direction=""out"" type=""s"" />
				</method>
				</interface>
				<interface name=""org.dbussharp.IntrospectableTest"">
				<method name=""Method1"">
				<arg name=""foo"" direction=""in"" type=""i"" />
				<arg name=""ret"" direction=""out"" type=""s"" />
				</method>
				<method name=""Method2"">
				<arg name=""value"" direction=""out"" type=""x"" />
				<annotation name=""org.freedesktop.DBus.Deprecated"" value=""true"" />
				</method>
				<property name=""Dict"" type=""a{ias}"" access=""read"" />
				</interface>
				</node>";

		[Test]
		public void SimpleInterfaceTest ()
		{
			intro.WriteStart ();
			intro.WriteType (typeof (ObjectIntrospectedImpl));
			intro.WriteEnd ();
			Assert.IsTrue (XNode.DeepEquals (XDocument.Parse (expectedOutputSimpleInterface),
			                                 XDocument.Parse (intro.Xml)));
		}

		[Test]
		public void InterfaceThroughWireTest ()
		{
			ObjectIntrospectedImpl impl = new ObjectIntrospectedImpl ();
			ObjectPath path = new ObjectPath ("/org/dbussharp/test");
			Bus.Session.Register (path, impl);

			const string ServiceName = "org.dbussharp.testservice";
			Bus.Session.RequestName (ServiceName);
			var iface = Bus.Session.GetObject<org.freedesktop.DBus.Introspectable> ("org.dbussharp.testservice", path);

			Assert.IsTrue (XNode.DeepEquals (XDocument.Parse (expectedOutputSimpleInterface),
			                                 XDocument.Parse (iface.Introspect ())));
		}
	}
}

