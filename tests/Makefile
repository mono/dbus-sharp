CSC=gmcs
RUNNER=nunit-console2

TESTS=AddressTest.cs AuthenticationTest.cs MatchRuleTest.cs ObjectPathTest.cs SignatureTest.cs

TESTLIB=NDesk.DBus.Tests.dll

NDesk.DBus.Tests.dll: $(TESTS)
	$(CSC) -debug -t:library -out:$@ -r:NDesk.DBus.dll -keyfile:../ndesk.snk -pkg:mono-nunit $(TESTS)

.PHONY: setup
setup:
	ln -sf ../src/NDesk.DBus.dll .
	ln -sf ../src/NDesk.DBus.dll.mdb .

.PHONY: run
run: $(TESTLIB)
	$(RUNNER) $(TESTLIB)
