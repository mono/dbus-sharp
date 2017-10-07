// Copyright 2017 Steffen Kiess <kiess@ki4.de>
// This software is made available under the MIT License
// See COPYING for details

using System;
using DBus;

public static class Constants {
	public const string BusName = "mono_dbus_sharp_test.UnixFDService";
	public static readonly ObjectPath ObjectPath = new ObjectPath ("/mono_dbus_sharp_test/UnixFDService");
}

[DBus.Interface ("mono_dbus_sharp_test.UnixFDService")]
public interface Interface {
	void Ping ();

	void ListOpenFDs ();

	byte[] GetBytes (int len);

	UnixFD GetFD (DisposableList disposableList, bool throwError);
	UnixFD[] GetFDList (DisposableList disposableList, bool throwError);
	object GetFDListVariant (DisposableList disposableList, bool throwError);

	void SendFD (UnixFD fd);
	void SendFDList (UnixFD[] fds);
	void SendFDListVariant (object fds);

	void RegisterSignalInterface (string busName, ObjectPath path);
}

[DBus.Interface ("mono_dbus_sharp_test.UnixFDService")]
public interface Interface2 {
	void Ping ();

	void ListOpenFDs ();

	byte[] GetBytes (int len);

	UnixFD GetFD (bool throwError);
	UnixFD[] GetFDList (bool throwError);
	object GetFDListVariant (bool throwError);

	void SendFD (UnixFD fd);
	void SendFDList (UnixFD[] fds);
	void SendFDListVariant (object fds);

	void RegisterSignalInterface (string busName, ObjectPath path);
}

[DBus.Interface ("mono_dbus_sharp_test.UnixFDSignals")]
public interface Signals {
	event Action<UnixFD> GotFD;
}

// vim: noexpandtab
// Local Variables:
// tab-width: 4
// c-basic-offset: 4
// indent-tabs-mode: t
// End:
