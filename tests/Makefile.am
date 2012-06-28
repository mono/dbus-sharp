ASSEMBLY = dbus-sharp-tests
TARGET = $(ASSEMBLY).dll
XBUILD_Q = $(XBUILD) /nologo /verbosity:quiet

NUNIT_CONSOLE = $$(echo $$(which nunit-console2 || which nunit-console))
RUNNER = $(NUNIT_CONSOLE) -nologo

if ENABLE_TESTS

all: $(TARGET)

$(TARGET):
	$(XBUILD_Q) dbus-sharp-tests.csproj

test: $(TARGET)
	$(RUNNER) $$TEST_HARNESS_FLAGS $(TARGET)

check: test

.PHONY: $(TARGET)

endif

EXTRA_DIST = \
	$(CSFILES)

CLEANFILES = \
	$(TARGET) \
	$(TARGET).mdb

MAINTAINERCLEANFILES = \
	Makefile.in
