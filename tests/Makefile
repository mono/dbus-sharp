CSC=gmcs
NUNIT_CONSOLE=$$(echo $$(which nunit-console2 || which nunit-console))
RUNNER=$(NUNIT_CONSOLE) -nologo

TESTS= \
	AddressTest.cs \
	AuthenticationTest.cs \
	ExportInterfaceTest.cs \
	ObjectPathTest.cs \
	RenamedInterfaceTest.cs \
	MatchRuleTest.cs \
	SignatureTest.cs \
	MessageReaderTest.cs \
	MessageWriterTest.cs

TESTLIB=dbus-sharp-tests.dll

$(TESTLIB): $(TESTS)
	$(CSC) -debug -t:library -out:$@ -r:dbus-sharp.dll -keyfile:../dbus-sharp.snk -pkg:mono-nunit $(TESTS)

.PHONY: setup run clean
setup:
	ln -sf ../src/dbus-sharp.dll .
	ln -sf ../src/dbus-sharp.dll.config .
	ln -sf ../src/dbus-sharp.dll.mdb .

run: $(TESTLIB)
	if test -z "$$DBUS_SESSION_BUS_ADDRESS" ; then \
		eval `dbus-launch --sh-syntax` ; \
		echo "D-Bus per-session daemon address is: $$DBUS_SESSION_BUS_ADDRESS" ; \
	fi; $(RUNNER) $(TESTLIB);

clean:
	rm $(TESTLIB)
