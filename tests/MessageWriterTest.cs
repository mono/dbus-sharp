
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
	}
}