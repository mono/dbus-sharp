
using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using DBus;
using DBus.Protocol;

namespace DBus.Tests
{
	[TestFixture]
	public class MessageReaderTest
	{
		[Test]
		public void ReadIntLittleEndian ()
		{
			MessageReader reader = new MessageReader (EndianFlag.Little, new byte[] { 8, 8, 0, 0});
			Assert.AreEqual (0x808, reader.ReadInt32 ());
			Assert.IsFalse (reader.DataAvailable);
		}

		[Test]
		public void ReadIntBigEndian ()
		{
			MessageReader reader = new MessageReader (EndianFlag.Big, new byte[] { 0, 0, 8, 8});
			Assert.AreEqual (0x808, reader.ReadInt32 ());
			Assert.IsFalse (reader.DataAvailable);
		}

		[Test]
		public void ReadIntArrayLittleEndian ()
		{
			byte[] data = new byte[] { 4, 0, 0, 0, 8, 8, 0, 0, 8, 8, 0, 0, 8, 8, 0, 0, 8, 8, 0, 0 };
			MessageReader reader = new MessageReader (EndianFlag.Little, data);
			
			int[] array = (int[])reader.ReadArray (typeof (int));
			Assert.IsNotNull (array);
			Assert.AreEqual (4, array.Length, "length");
			CollectionAssert.AreEqual (new int[] { 0x808, 0x808, 0x808, 0x808}, array, "elements");
			Assert.IsFalse (reader.DataAvailable);
		}

		[Test]
		public void ReadIntArrayBigEndian ()
		{
			byte[] data = new byte[] { 0, 0, 0, 4, 0, 0, 8, 8, 0, 0, 8, 8, 0, 0, 8, 8, 0, 0, 8, 8 };
			MessageReader reader = new MessageReader (EndianFlag.Big, data);
			
			int[] array = (int[])reader.ReadArray (typeof (int));
			Assert.IsNotNull (array);
			Assert.AreEqual (4, array.Length, "length");
			CollectionAssert.AreEqual (new int[] { 0x808, 0x808, 0x808, 0x808}, array, "elements");
			Assert.IsFalse (reader.DataAvailable);
		}

		[Test]
		public void ReadBooleanArrayLittleEndian ()
		{
			byte[] data = new byte[] { 4, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 };
			MessageReader reader = new MessageReader (EndianFlag.Little, data);
			
			bool[] array = (bool[])reader.ReadArray (typeof (bool));
			Assert.IsNotNull (array);
			Assert.AreEqual (4, array.Length, "length");
			CollectionAssert.AreEqual (new bool[] { true, false, true, true}, array, "elements");
			Assert.IsFalse (reader.DataAvailable);
		}

		[Test]
		public void ReadBooleanArrayBigEndian ()
		{
			byte[] data = new byte[] { 0, 0, 0, 4, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1 };
			MessageReader reader = new MessageReader (EndianFlag.Big, data);
			
			bool[] array = (bool[])reader.ReadArray (typeof (bool));
			Assert.IsNotNull (array);
			Assert.AreEqual (4, array.Length, "length");
			CollectionAssert.AreEqual (new bool[] { true, false, true, true}, array, "elements");
			Assert.IsFalse (reader.DataAvailable);
		}

		[StructLayout (LayoutKind.Sequential)]
		struct TestStruct {
			public int Item1;
			public long Item2;
			public int Item3;
		}

		[Test]
		public void ReadIntLongIntStructLittleEndian ()
		{
			// (ixi) and (1, 2, 3)
			byte[] data = new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0 };
			MessageReader reader = new MessageReader (EndianFlag.Little, data);

			TestStruct stct = (TestStruct)reader.ReadStruct (typeof (TestStruct));
			Assert.AreEqual (1, stct.Item1);
			Assert.AreEqual (2, stct.Item2);
			Assert.AreEqual (3, stct.Item3);			
		}

		[Test, ExpectedException (typeof (MessageReader.PaddingException))]
		public void ReadIntLongIntStructNonAlignedLittleEndian ()
		{
			// (ixi) and (1, 2, 3)
			byte[] data = new byte[] { 1, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0 };
			MessageReader reader = new MessageReader (EndianFlag.Little, data);

			TestStruct stct = (TestStruct)reader.ReadStruct (typeof (TestStruct));
		}

	}
}
	