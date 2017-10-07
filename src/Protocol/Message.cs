// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;

namespace DBus.Protocol
{
	public class Message : IDisposable
	{
		Header header = new Header ();
		Connection connection;
		byte[] body;
		UnixFDArray fdArray;

		public Message ()
		{
			header.Endianness = Connection.NativeEndianness;
			header.MessageType = MessageType.MethodCall;
			header.Flags = HeaderFlag.NoReplyExpected; //TODO: is this the right place to do this?
			header.MajorVersion = ProtocolInformation.Version;
		}

		public static Message FromReceivedBytes (Connection connection, byte[] header, byte[] body)
		{
			return FromReceivedBytes (connection, header, body, null);
		}
		
		public static Message FromReceivedBytes (Connection connection, byte[] header, byte[] body, UnixFDArray fdArray)
		{
			Message message = new Message ();
			message.connection = connection;
			message.body = body;
			message.fdArray = fdArray;
			message.SetHeaderData (header);

			return message;
		}

		public byte[] Body {
			get {
				return body;
			}
		}

		public UnixFDArray UnixFDArray {
			get {
				return fdArray;
			}
		}

		public Header Header {
			get {
				return header;
			}
		}

		public Connection Connection {
			get {
				return connection;
			}
		}

		public Signature Signature {
			get {
				object o = Header[FieldCode.Signature];
				if (o == null)
					return Signature.Empty;
				else
					return (Signature)o;
			}
			set {
				if (value == Signature.Empty)
					Header[FieldCode.Signature] = null;
				else
					Header[FieldCode.Signature] = value;
			}
		}

		public bool ReplyExpected {
			get {
				return (Header.Flags & HeaderFlag.NoReplyExpected) == HeaderFlag.None;
			}
			set {
				if (value)
					Header.Flags &= ~HeaderFlag.NoReplyExpected; //flag off
				else
					Header.Flags |= HeaderFlag.NoReplyExpected; //flag on
			}
		}

		public void AttachBodyTo (MessageWriter writer)
		{
			body = writer.ToArray ();
			header.Length = (uint)body.Length;
			if (writer.fdArray.FDs.Count != 0) {
				header[FieldCode.UnixFDs] = (uint) writer.fdArray.FDs.Count;
				if (fdArray == null)
					fdArray = new UnixFDArray ();
				foreach (var fd in writer.fdArray.FDs)
					fdArray.FDs.Add (fd);
			}
		}

		public void HandleHeader (Header headerIn)
		{
			header = headerIn;
		}

		public void SetHeaderData (byte[] data)
		{
			header = Header.FromBytes (data);
		}

		public byte[] GetHeaderData ()
		{
			MessageWriter writer = new MessageWriter (header.Endianness);
			header.WriteHeaderToMessage (writer);
			return writer.ToArray ();
		}

		public void Dispose ()
		{
			if (UnixFDArray != null)
				UnixFDArray.Dispose ();
		}
	}
}
