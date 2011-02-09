
using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using DBus;
using DBus.Protocol;

namespace DBus.Tests
{
	[TestFixture]
	public class MessageWriterTest
	{
		DBus.Protocol.MessageWriter writer;

		[SetUp]
		public void Setup ()
		{
			writer = new DBus.Protocol.MessageWriter ();
		}

		[Test]
		public void WriteIntArrayTest ()
		{
			var initial = new int[] { 1, 2, 3, 4 };
			writer.WriteArray<int> (initial);
			byte[] result = writer.ToArray ();

			Assert.AreEqual (4 + initial.Length * 4, result.Length);
			uint length = BitConverter.ToUInt32 (result, 0);
			Assert.AreEqual (initial.Length * 4, length);
			for (int i = 0; i < initial.Length; i++)
				Assert.AreEqual (i + 1, BitConverter.ToInt32 (result, 4 + 4 * i), "#" + i);
		}

		struct TestStruct
		{
			public int bleh;
			public uint bloup;
			public float blop;
		}

		[Test]
		public void WriteStructTest ()
		{
			TestStruct stct = new TestStruct ();
			stct.bleh = 5;
			stct.bloup = 3;
			stct.blop = 5.5f;

			writer.WriteStructure<TestStruct> (stct);
			byte[] result = writer.ToArray ();
			Assert.AreEqual (5, BitConverter.ToInt32 (result, 0));
			Assert.AreEqual ((uint)3, BitConverter.ToUInt32 (result, 4));
			Assert.AreEqual (5.5f, BitConverter.ToSingle (result, 8));
		}
	}
}