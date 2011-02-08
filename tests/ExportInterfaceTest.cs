// Copyright 2007 Alp Toker <alp@atoker.com>
//           2011 Bertrand Lorentz <bertrand.lorentz@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;

using NUnit.Framework;
using DBus;
using org.freedesktop.DBus;

namespace DBus.Tests
{
	[TestFixture]
	public class ExportInterfaceTest
	{
		ITestOne test;
		Test test_server;

		string bus_name = "org.dbussharp.test";
		ObjectPath path = new ObjectPath ("/org/dbussharp/test");
		
		int event_a_count = 0;
        
		[TestFixtureSetUp]
        public void Setup ()
        {
			test_server = new Test ();
			Bus.Session.Register (path, test_server);
			Assert.AreEqual (Bus.Session.RequestName (bus_name), RequestNameReply.PrimaryOwner);
			
			Assert.AreNotEqual (Bus.Session.RequestName (bus_name), RequestNameReply.PrimaryOwner);
			test = Bus.Session.GetObject<ITestOne> (bus_name, path);
        }
		
		/// <summary>
		/// 
		/// </summary>
		[Test]
		public void VoidMethods ()
		{
			test.VoidObject (1);
			Assert.IsTrue (test_server.void_object_called);
			
			test.VoidEnums (TestEnum.Bar, TestEnum.Foo);
			Assert.IsTrue (test_server.void_enums_called);

			test.VoidString ("foo");
			Assert.IsTrue (test_server.void_enums_called);
		}

		/// <summary>
		/// 
		/// </summary>
		[Test]
		public void FireEvent ()
		{
			test.SomeEvent += HandleSomeEventA;
			test.FireSomeEvent ();
			Console.WriteLine ("fired-EventA");
			Bus.Session.Iterate ();
			Assert.AreEqual (1, event_a_count);
			
			test.SomeEvent -= HandleSomeEventA;
			test.FireSomeEvent ();
			Bus.Session.Iterate ();
			
			Assert.AreEqual (1, event_a_count);
		}
		
		private void HandleSomeEventA (string arg1, object arg2, double arg3, MyTuple mt)
		{
			event_a_count++;
			Console.WriteLine ("EventA");
		}

		/// <summary>
		/// 
		/// </summary>
		[Test]
		public void GetVariant ()
		{
			Assert.IsInstanceOfType (typeof (byte []), test.GetSomeVariant ());
		}
		
		/// <summary>
		/// 
		/// </summary>
		[Test]
		public void WithOutParameters ()
		{
			uint n;
			string istr = "21";
			string ostr;
			test.WithOutParameters (out n, istr, out ostr);
			Assert.AreEqual (UInt32.Parse (istr), n);
			Assert.AreEqual ("." + istr + ".", ostr);
			
			uint[] a1, a2, a3;
			test.WithOutParameters2 (out a1, out a2, out a3);
			Assert.AreEqual (new uint[] { }, a1);
			Assert.AreEqual (new uint[] { 21, 23, 16 }, a2);
			Assert.AreEqual (new uint[] { 21, 23 }, a3);
		}
				
		/// <summary>
		/// 
		/// </summary>
		[Test]
		public void GetPresences ()
		{
			uint[] @contacts = new uint[] { 2 };
			IDictionary<uint,SimplePresence> presences;
			test.GetPresences (contacts, out presences);
			presences[2] = new SimplePresence { Type = ConnectionPresenceType.Offline, Status = "offline", StatusMessage = "" };
			var presence = presences[2];
			Assert.AreEqual (ConnectionPresenceType.Offline, presence.Type);
			Assert.AreEqual ("offline", presence.Status);
			Assert.AreEqual ("", presence.StatusMessage);
		}
		
		/// <summary>
		/// 
		/// </summary>
		[Test]
		public void ReturnValues ()
		{
			string str = "abcd";
			Assert.AreEqual (str.Length, test.StringLength (str));
		}
		
		/// <summary>
		/// 
		/// </summary>
		[Test]
		public void ComplexAsVariant ()
		{
			MyTuple2 cpx = new MyTuple2 ();
			cpx.A = "a";
			cpx.B = "b";
			cpx.C = new Dictionary<int,MyTuple> ();
			cpx.C[3] = new MyTuple("foo", "bar");
			object cpxRet = test.ComplexAsVariant (cpx, 12);
			MyTuple2 mt2ret = (MyTuple2)Convert.ChangeType (cpxRet, typeof (MyTuple2));
			Assert.AreEqual (cpx.A, mt2ret.A);
			Assert.AreEqual (cpx.B, mt2ret.B);
			Assert.AreEqual (cpx.C[3].A, mt2ret.C[3].A);
			Assert.AreEqual (cpx.C[3].B, mt2ret.C[3].B);
		}

		/// <summary>
		/// 
		/// </summary>
		[Test]
		public void Property ()
		{
			int i = 99;
			test.SomeProp = i;
			Assert.AreEqual (i, test.SomeProp);
			test.SomeProp = i + 1;
			Assert.AreEqual (i + 1, test.SomeProp);
		}

		/// <summary>
		/// 
		/// </summary>
		[Test]
		public void CatchException ()
		{
			try {
				test.ThrowSomeException ();
				Assert.Fail ("An exception should be thrown before getting here");
			} catch (Exception ex) {
				Assert.IsTrue (ex.Message.Contains ("Some exception"));
			}
		}
		
		/* Locks up ?
		/// <summary>
		/// 
		/// </summary>
		[Test]
		public void ObjectArray ()
		{
			string str = "The best of times";
			ITestOne[] objs = test.GetObjectArray ();
			foreach (ITestOne obj in objs) {
				Assert.AreEqual (str.Length, obj.StringLength (str));
			}
		}*/
	}
	
	public delegate void SomeEventHandler (string arg1, object arg2, double arg3, MyTuple mt);

	[Interface ("org.dbussharp.test")]
	public interface ITestOne
	{
		event SomeEventHandler SomeEvent;
		void FireSomeEvent ();
		void VoidObject (object obj);
		int StringLength (string str);
		void VoidEnums (TestEnum a, TestEnum b);
		void VoidString (string str);
		object GetSomeVariant ();
		void ThrowSomeException ();
		void WithOutParameters (out uint n, string str, out string ostr);
		void WithOutParameters2 (out uint[] a1, out uint[] a2, out uint[] a3);
		void GetPresences (uint[] @contacts, out IDictionary<uint,SimplePresence> @presence);
		object ComplexAsVariant (object v, int num);
	
		ITestOne[] GetEmptyObjectArray ();
		ITestOne[] GetObjectArray ();
		int SomeProp { get; set; }
	}
	
	public class Test : ITestOne
	{
		public event SomeEventHandler SomeEvent;
		
		public bool void_enums_called = false;
		public bool void_object_called = false;
		public bool void_string_called = false;
	
		public void VoidObject (object var)
		{
			void_object_called = true;
		}
		
		public int StringLength (string str)
		{
			return str.Length;
		}
	
		public void VoidEnums (TestEnum a, TestEnum b)
		{
			void_enums_called = true;
		}
	
		public virtual void VoidString (string str)
		{
			void_string_called = true;
		}
	
		public void FireSomeEvent ()
		{
			MyTuple mt;
			mt.A = "a";
			mt.B = "b";
	
			if (SomeEvent != null) {
				SomeEvent ("some string", 21, 19.84, mt);
			}
		}
	
		public object GetSomeVariant ()
		{
			return new byte[0];
		}
	
		public void ThrowSomeException ()
		{
			throw new Exception ("Some exception");
		}
	
		public void WithOutParameters (out uint n, string str, out string ostr)
		{
			n = UInt32.Parse (str);
			ostr = "." + str + ".";
		}
	
		public void WithOutParameters2 (out uint[] a1, out uint[] a2, out uint[] a3)
		{
			a1 = new uint[] { };
			a2 = new uint[] { 21, 23, 16 };
			a3 = new uint[] { 21, 23 };
		}
	
		public void GetPresences (uint[] @contacts, out IDictionary<uint,SimplePresence> @presence)
		{
			Dictionary<uint,SimplePresence> presences = new Dictionary<uint,SimplePresence>();
			presences[2] = new SimplePresence { Type = ConnectionPresenceType.Offline, Status = "offline", StatusMessage = "" };
			presence = presences;
		}
	
		public object ComplexAsVariant (object v, int num)
		{
			MyTuple2 mt2 = (MyTuple2)Convert.ChangeType (v, typeof (MyTuple2));
	
			return mt2;
		}
	
		public ITestOne[] GetEmptyObjectArray ()
		{
			return new Test[] {};
		}
	
		public ITestOne[] GetObjectArray ()
		{
			return new ITestOne[] {this};
		}
	
		public int SomeProp { get; set; }
	}

	public enum TestEnum : byte
	{
		Foo,
		Bar,
	}

	public struct MyTuple
	{
		public MyTuple (string a, string b)
		{
			A = a;
			B = b;
		}
	
		public string A;
		public string B;
	}

	public struct MyTuple2
	{
		public string A;
		public string B;
		public IDictionary<int,MyTuple> C;
	}

	public enum ConnectionPresenceType : uint
	{
		Unset = 0, Offline = 1, Available = 2, Away = 3, ExtendedAway = 4, Hidden = 5, Busy = 6, Unknown = 7, Error = 8, 
	}

	public struct SimplePresence
	{
		public ConnectionPresenceType Type;
		public string Status;
		public string StatusMessage;
	}
}
