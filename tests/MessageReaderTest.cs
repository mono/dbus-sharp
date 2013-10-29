
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
			byte[] data = new byte[] { 16, 0, 0, 0, 8, 8, 0, 0, 8, 8, 0, 0, 8, 8, 0, 0, 8, 8, 0, 0 };
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
			byte[] data = new byte[] { 0, 0, 0, 16, 0, 0, 8, 8, 0, 0, 8, 8, 0, 0, 8, 8, 0, 0, 8, 8 };
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

		[StructLayout (LayoutKind.Sequential)]
		struct TestStruct2 {
			public int Item1;
			public int Item2;
			public int Item3;
		}

		[Test]
		public void ReadIntIntIntStructLittleEndian ()
		{
			// Will test the fast path
			byte[] data = new byte[] { 1, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0 };
			MessageReader reader = new MessageReader (EndianFlag.Little, data);

			TestStruct2 stct = (TestStruct2)reader.ReadStruct (typeof (TestStruct2));
			Assert.AreEqual (1, stct.Item1);
			Assert.AreEqual (2, stct.Item2);
			Assert.AreEqual (3, stct.Item3);
		}

		[StructLayout (LayoutKind.Sequential)]
		struct TestStruct3 {
			public long Item1;
			public ulong Item2;
			public double Item3;
		}

		[Test]
		public void ReadSameAlignementStructNativeEndian ()
		{
			// Will test the fast path with mixed types but same alignement
			byte[] data = new byte[8 * 3];
			Array.Copy (BitConverter.GetBytes ((long)1), 0, data, 0, 8);
			Array.Copy (BitConverter.GetBytes (ulong.MaxValue), 0, data, 8, 8);
			Array.Copy (BitConverter.GetBytes ((double)3.3), 0, data, 16, 8);

			MessageReader reader = new MessageReader (BitConverter.IsLittleEndian ? EndianFlag.Little : EndianFlag.Big, data);

			TestStruct3 stct = (TestStruct3)reader.ReadStruct (typeof (TestStruct3));
			Assert.AreEqual (1, stct.Item1);
			Assert.AreEqual (ulong.MaxValue, stct.Item2);
			Assert.AreEqual (3.3, stct.Item3);
		}

		[Test]
		public void ReadSameAlignementStructNonNativeEndian ()
		{
			// Will test the fast path with mixed types but same alignement
			byte[] data = new byte[8 * 3];
			Array.Copy (BitConverter.GetBytes ((long)1), 0, data, 0, 8);
			Array.Copy (BitConverter.GetBytes (ulong.MaxValue), 0, data, 8, 8);
			Array.Copy (BitConverter.GetBytes ((double)3.3), 0, data, 16, 8);
			// Swap value to simulate other endianess
			for (int i = 0; i < data.Length; i += 8) {
				for (int j = 0; j < 4; j++) {
					data[i + j] = (byte)(data[i + j] ^ data[i + 7 - j]);
					data[i + 7 - j] = (byte)(data[i + j] ^ data[i + 7 - j]);
					data[i + j] = (byte)(data[i + j] ^ data[i + 7 - j]);
				}
			}

			MessageReader reader = new MessageReader (!BitConverter.IsLittleEndian ? EndianFlag.Little : EndianFlag.Big, data);

			TestStruct3 stct = (TestStruct3)reader.ReadStruct (typeof (TestStruct3));
			Assert.AreEqual (1, stct.Item1);
			Assert.AreEqual (ulong.MaxValue, stct.Item2);
			Assert.AreEqual (3.3, stct.Item3);
		}
	}
}
