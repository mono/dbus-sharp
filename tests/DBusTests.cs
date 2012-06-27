using System;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;
using DBus;
using org.freedesktop.DBus;

namespace DBus.Tests
{
	[TestFixture]
	public class DBusTests
	{
		[Test]
		public void TestIntrospectable ()
		{
			var introspectable = Bus.Session.GetObject<Introspectable> ("org.freedesktop.DBus", ObjectPath.Root);
			var xml = introspectable.Introspect ();
			Assert.IsNotNull (xml);
			Assert.IsNotEmpty (xml);

			var doc = XDocument.Parse (xml);
			Assert.AreEqual ("node", doc.Root.Name);
			// the main dbus object has two interfaces, the dbus interface and the introspectable one
			Assert.AreEqual (2, doc.Elements ("interface").Count ());
			var iface = doc.Elements ("interface").FirstOrDefault (e => e.Attribute ("name") == "org.freedesktop.DBus.Introspectable");
			Assert.IsNotNull (iface);
			Assert.AreEqual (1, iface.Elements ("method").Count ());
			Assert.AreEqual ("Introspect", iface.Element ("method").Attribute ("name"));
			Assert.AreEqual (1, iface.Element ("method").Elements ("arg").Count ());
		}
	}
}
