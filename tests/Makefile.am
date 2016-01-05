ASSEMBLY = dbus-sharp-tests
TARGET = $(ASSEMBLY).dll
XBUILD_Q = $(XBUILD) /p:TargetFrameworkVersion="4.5" /nologo /verbosity:quiet

NUNIT_CONSOLE = $$(echo $$(which nunit-console2 || which nunit-console))
RUNNER = $(NUNIT_CONSOLE) -nologo

CSFILES = AddressTest.cs \
	AuthenticationTest.cs \
	MatchRuleTest.cs \
	ObjectPathTest.cs \
	SignatureTest.cs \
	BusTests.cs \
	ExportInterfaceTest.cs \
	RenamedInterfaceTest.cs \
	MessageReaderTest.cs \
	MessageWriterTest.cs \
	IntrospectorTest.cs

if ENABLE_TESTS

all: $(TARGET)

$(TARGET):
	$(XBUILD_Q) dbus-sharp-tests.csproj

test: $(TARGET)
	if test -z "$$DBUS_SESSION_BUS_ADDRESS" ; then \
		eval `dbus-launch --sh-syntax` ; \
		echo "D-Bus per-session daemon address is: $$DBUS_SESSION_BUS_ADDRESS" ; \
	fi; $(RUNNER) $$TEST_HARNESS_FLAGS $(TARGET);

check: test

# This is not needed on that branch
# it's for compat with wrench
setup: ;
run: check

.PHONY: $(TARGET)

endif

EXTRA_DIST = \
	$(CSFILES) \
	dbus-sharp-tests.csproj

CLEANFILES = \
	$(TARGET) \
	$(TARGET).mdb

MAINTAINERCLEANFILES = \
	Makefile.in
