DBUS_SHARP_PREFIX=..

VPATH=$(DBUS_SHARP_PREFIX)/src

all: run test.exe test-sample.exe test-export.exe test-exceptions.exe test-notifications.exe

test.exe: NDesk.DBus.dll Test.cs

test-sample.exe: NDesk.DBus.dll TestSample.cs

test-export.exe: NDesk.DBus.dll TestExport.cs

test-exceptions.exe: NDesk.DBus.dll TestExceptions.cs

test-server.exe: REFS=Mono.Posix

test-server.exe: NDesk.DBus.dll TestServer.cs

test-notifications.exe: NDesk.DBus.dll Notifications.cs TestNotifications.cs

run:
	echo '#!/bin/sh' > $@
	echo 'MONO_PATH=$(DBUS_SHARP_PREFIX)/src:$$MONO_PATH' >> $@
	echo '$(RUNTIME) $$@' >> $@
	chmod +x $@


include ../include.mk
