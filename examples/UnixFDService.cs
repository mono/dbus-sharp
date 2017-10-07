// Copyright 2017 Steffen Kiess <kiess@ki4.de>
// This software is made available under the MIT License
// See COPYING for details

/*
PATH="$HOME/mono-master/bin:$PATH" ./autogen.sh && make -j8
make && ( cd examples && mcs -g UnixFDService.cs UnixFDInterface.cs -r ../src/dbus-sharp.dll -r Mono.Posix ) && MONO_PATH=src $HOME/mono-master/bin/mono --debug examples/UnixFDService.exe
make && ( cd examples && mcs -g UnixFDClient.cs UnixFDInterface.cs -r ../src/dbus-sharp.dll -r Mono.Posix ) && MONO_PATH=src $HOME/mono-master/bin/mono --debug examples/UnixFDClient.exe

python3
import dbus; dbus.SessionBus().get_object('mono_dbus_sharp_test.UnixFDService', '/mono_dbus_sharp_test/UnixFDService').GetBytes(3)
import dbus; f = dbus.SessionBus().get_object('mono_dbus_sharp_test.UnixFDService', '/mono_dbus_sharp_test/UnixFDService').GetFD ()
*/

using System;

using Mono.Unix;
using Mono.Unix.Native;

using DBus;
using org.freedesktop.DBus;

public class Impl : Interface {
	Connection conn;

	public Impl (Connection conn)
	{
		this.conn = conn;
	}

	public void Ping ()
	{
	}

	public void ListOpenFDs ()
	{
		Console.WriteLine ("Open FDs:");
		Mono.Unix.Native.Stdlib.system ("ls -l /proc/$PPID/fd/");
	}

	public UnixFD GetFD (DisposableList disposableList, bool throwError)
	{
		var fd_ = Syscall.open ("/dev/null", OpenFlags.O_RDWR, 0);
		if (fd_ < 0)
			UnixMarshal.ThrowExceptionForLastError ();
		var fd = new UnixFD (fd_);
		disposableList.Add (fd);

		if (throwError)
			throw new Exception ("Throwing an exception after creating a UnixFD object");

		return fd;
	}

	public UnixFD[] GetFDList (DisposableList disposableList, bool throwError)
	{
		return new UnixFD[] {
			GetFD (disposableList, false), GetFD (disposableList, throwError), GetFD (disposableList, false)
		};
	}

	public object GetFDListVariant (DisposableList disposableList, bool throwError)
	{
		return GetFDList (disposableList, throwError);
	}

	public byte[] GetBytes (int len)
	{
		return new byte[len];
	}

	public void SendFD (UnixFD fd)
	{
		Console.WriteLine ("Got FD as parameter:");
		Mono.Unix.Native.Stdlib.system ("ls -l /proc/$PPID/fd/" + fd.Handle);
	}

	public void SendFDList (UnixFD[] fds)
	{
		Console.WriteLine ("Got FDs as parameter:");
		foreach (var fd in fds)
			Mono.Unix.Native.Stdlib.system ("ls -l /proc/$PPID/fd/" + fd.Handle);
	}

	public void SendFDListVariant (object fds)
	{
		Console.WriteLine ("Got FDs as variant parameter:");
		foreach (var fd in (UnixFD[]) fds)
			Mono.Unix.Native.Stdlib.system ("ls -l /proc/$PPID/fd/" + fd.Handle);
	}

	public void RegisterSignalInterface (string busName, ObjectPath path)
	{
		Console.WriteLine ("Register for GotFD event at {0} / {1}", busName, path);
		conn.GetObject<Signals> (busName, path).GotFD += fd => {
			Console.WriteLine ("Got FD from signal:");
			Mono.Unix.Native.Stdlib.system ("ls -l /proc/$PPID/fd/" + fd.Handle);
		};
	}
}

public class UnixFDService
{
	public static void Main (string[] args)
	{
		Bus conn;

		if (args.Length == 0)
			conn = Bus.Session;
		else {
			if (args[0] == "--session")
				conn = Bus.Session;
			else if (args[0] == "--system")
				conn = Bus.System;
			else
				conn = Bus.Open (args[0]);
		}

		conn.Register (Constants.ObjectPath, new Impl (conn));

		if (conn.RequestName (Constants.BusName) != org.freedesktop.DBus.RequestNameReply.PrimaryOwner)
			throw new Exception ("Could not request name");

		Console.WriteLine ("Waiting for requests...");

		while (conn.IsConnected) {
			conn.Iterate ();
		}
	}
}

// vim: noexpandtab
// Local Variables:
// tab-width: 4
// c-basic-offset: 4
// indent-tabs-mode: t
// End:
