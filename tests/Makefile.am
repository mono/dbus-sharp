ASSEMBLY = dbus-sharp-tests
TARGET = $(ASSEMBLY).dll

CSFLAGS = \
	-debug -t:library -d:STRONG_NAME -keyfile:$(top_srcdir)/dbus-sharp.snk

LIBFLAGS = \
	-r:dbus-sharp.dll \
	$(NUNIT_LIBS)

DBUS_SHARP_ASSEMBLY = $(top_builddir)/src/dbus-sharp.dll

NUNIT_CONSOLE = $$(echo $$(which nunit-console2 || which nunit-console))
RUNNER = $(NUNIT_CONSOLE) -nologo

CSFILES = \
	AddressTest.cs \
	AuthenticationTest.cs \
	BusTests.cs \
	ExportInterfaceTest.cs \
	MatchRuleTest.cs \
	ObjectPathTest.cs \
	RenamedInterfaceTest.cs \
	SignatureTest.cs

if ENABLE_TESTS

all: $(TARGET)

$(TARGET): $(CSFILES)
	ln -sf $(DBUS_SHARP_ASSEMBLY) .
	ln -sf $(DBUS_SHARP_ASSEMBLY).config .
	ln -sf $(DBUS_SHARP_ASSEMBLY).mdb .
	$(GMCS) -out:$@ $(CSFLAGS) $(LIBFLAGS) $^

test: $(TARGET)
	$(RUNNER) $(TARGET)

endif

EXTRA_DIST = \
	$(CSFILES)

CLEANFILES = \
	$(TARGET) \
	$(TARGET).mdb

MAINTAINERCLEANFILES = \
	Makefile.in
