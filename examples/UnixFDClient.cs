// Copyright 2017 Steffen Kiess <kiess@ki4.de>
// This software is made available under the MIT License
// See COPYING for details

using System;

using DBus;
using org.freedesktop.DBus;

using Mono.Unix;
using Mono.Unix.Native;

class SignalsImpl : Signals {
	public event Action<UnixFD> GotFD;

	public void CallGotFD (UnixFD fd) {
		var handler = GotFD;
		if (handler != null)
			handler (fd);
	}
}

public class ManagedDBusTest
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

		IBus bus = conn.GetObject<IBus> ("org.freedesktop.DBus", new ObjectPath ("/org/freedesktop/DBus"));
		Console.WriteLine (bus.ListNames ().Length);

		var obj = conn.GetObject<Interface> (Constants.BusName, Constants.ObjectPath);
		var obj2 = conn.GetObject<Interface2> (Constants.BusName, Constants.ObjectPath);
		var objIntr = conn.GetObject<Introspectable> (Constants.BusName, Constants.ObjectPath);
		obj.Ping ();
		Console.WriteLine (obj.GetBytes (3).Length);

		Console.WriteLine ("conn.UnixFDSupported = " + conn.UnixFDSupported);
		if (!conn.UnixFDSupported)
			return;

		using (var disposableList = new DisposableList ()) {
			var res = obj.GetFD (disposableList, false);
			Console.WriteLine ("Got FD:");
			Mono.Unix.Native.Stdlib.system ("ls -l /proc/$PPID/fd/" + res.Handle);
		}
		using (var disposableList = new DisposableList ()) {
			var res = obj.GetFDList (disposableList, false);
			Console.WriteLine ("Got FDs:");
			foreach (var fd in res)
				Mono.Unix.Native.Stdlib.system ("ls -l /proc/$PPID/fd/" + fd.Handle);
		}
		using (var disposableList = new DisposableList ()) {
			var res = (UnixFD[]) obj.GetFDListVariant (disposableList, false);
			Console.WriteLine ("Got FDs as variant:");
			foreach (var fd in res)
				Mono.Unix.Native.Stdlib.system ("ls -l /proc/$PPID/fd/" + fd.Handle);
		}

		using (var disposableList = new DisposableList ()) {
			try {
				obj.GetFD (disposableList, true);
				throw new Exception ("Expected an exception");
			} catch (Exception e) {
				if (!e.Message.Contains ("Throwing an exception after creating a UnixFD object"))
					throw;
			}
		}
		using (var disposableList = new DisposableList ()) {
			try {
				obj.GetFDList (disposableList, true);
				throw new Exception ("Expected an exception");
			} catch (Exception e) {
				if (!e.Message.Contains ("Throwing an exception after creating a UnixFD object"))
					throw;
			}
		}
		using (var disposableList = new DisposableList ()) {
			try {
				obj.GetFDListVariant (disposableList, true);
				throw new Exception ("Expected an exception");
			} catch (Exception e) {
				if (!e.Message.Contains ("Throwing an exception after creating a UnixFD object"))
					throw;
			}
		}

		// Check whether this leaks an FD
		obj.GetFD (null, false);
		obj.GetFDList (null, false);
		obj.GetFDListVariant (null, false);
		try { obj.GetFD (null, true); } catch {}
		try { obj.GetFDList (null, true); } catch {}
		try { obj.GetFDListVariant (null, true); } catch {}
		obj2.GetFD (false);
		obj2.GetFDList (false);
		obj2.GetFDListVariant (false);
		try { obj2.GetFD (true); } catch {}
		try { obj2.GetFDList (true); } catch {}
		try { obj2.GetFDListVariant (true); } catch {}

		var fd_ = Syscall.open ("/dev/null", OpenFlags.O_RDWR, 0);
		if (fd_ < 0)
			UnixMarshal.ThrowExceptionForLastError ();
		using (var fd = new UnixFD (fd_)) {
			obj.SendFD (fd);
			obj.SendFD (fd);
			obj.SendFDList (new UnixFD[] { fd, fd });
			obj.SendFDListVariant (new UnixFD[] { fd, fd });

			var impl = new SignalsImpl ();
			var spath = new ObjectPath ("/mono_dbus_sharp_test/Signals");
			conn.Register (spath, impl);
			obj.RegisterSignalInterface (conn.UniqueName, spath);
			impl.CallGotFD (fd);
		}

		Console.WriteLine (objIntr.Introspect ().Length);

		obj.ListOpenFDs ();
		Console.WriteLine ("Open FDs:");
		Mono.Unix.Native.Stdlib.system ("ls -l /proc/$PPID/fd/");
	}
}

// vim: noexpandtab
// Local Variables:
// tab-width: 4
// c-basic-offset: 4
// indent-tabs-mode: t
// End:
