all: dbus-monitor.exe

CSC=mcs
CSFLAGS=-debug

dbus-monitor.exe: Monitor.cs
	$(CSC) $(CSFLAGS) -keyfile:../dbus-sharp.snk -r:../src/dbus-sharp.dll -out:$@ $^

