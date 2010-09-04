all: dbus-monitor.exe

CSC=gmcs
CSFLAGS=-debug

dbus-monitor.exe: Monitor.cs
	$(CSC) $(CSFLAGS) -keyfile:../ndesk.snk -r:../src/dbus-sharp.dll -out:$@ $^

