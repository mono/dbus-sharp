DBUS_SHARP_PREFIX=..

VPATH=$(DBUS_SHARP_PREFIX)/src

all: dbus-monitor

dbus-monitor.exe: NDesk.DBus.dll Monitor.cs

uuidgen.exe: NDesk.DBus.dll UuidGen.cs

introspect.exe: NDesk.DBus.dll Introspect.cs IntrospectionSchema.cs

dbus-monitor: dbus-monitor.exe
	echo '#!/bin/sh' > $@
	echo 'export MONO_PATH=$(DBUS_SHARP_PREFIX)/src:$$MONO_PATH' >> $@
	echo '$(RUNTIME) $^ $$@' >> $@
	chmod +x $@


include ../include.mk
