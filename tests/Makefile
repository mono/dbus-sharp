CSC=gmcs
RUNNER=nunit-console2

TESTS=AddressTest.cs AuthenticationTest.cs MatchRuleTest.cs ObjectPathTest.cs SignatureTest.cs

TESTLIB=dbus-sharp-tests.dll

dbus-sharp-tests.dll: $(TESTS)
	$(CSC) -debug -t:library -out:$@ -r:dbus-sharp.dll -keyfile:../dbus-sharp.snk -pkg:mono-nunit $(TESTS)

.PHONY: setup
setup:
	ln -sf ../src/dbus-sharp.dll .
	ln -sf ../src/dbus-sharp.dll.mdb .

.PHONY: run
run: $(TESTLIB)
	$(RUNNER) $(TESTLIB)
