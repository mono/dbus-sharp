#CSC_DEBUGFLAGS=-debug -d:TRACE
CSC_DEBUGFLAGS=-debug
CSC=gmcs $(CSC_DEBUGFLAGS)
MONO_DEBUGFLAGS=--debug
RUNTIME=mono $(MONO_DEBUGFLAGS)
GACUTIL=gacutil
GACUTIL_FLAGS=-root $(DESTDIR)$(prefix)/lib
#this isn't great
prefix=$(shell dirname `which gacutil`)/..

#%.exe:
%.exe %.dll %.module:
	$(CSC) $(CSFLAGS) -out:$@ -t:$(TARGET) $(addprefix -pkg:,$(PKGS)) $(addprefix -r:,$(REFS)) $(addprefix -r:,$(filter %.dll,$^)) $(addprefix -addmodule:,$(filter %.module,$^)) $(filter %.cs,$^)

%.exe: TARGET = exe

%.dll: TARGET = library

%.module: TARGET = module

#$(MODULE)_SOURCES := foo.cs

