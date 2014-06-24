SUBDIRS = src tools examples tests

test:
if ENABLE_TESTS
	$(MAKE) -C tests setup run
else
	@echo "Test are disabled, run ./configure with --enable-tests"
endif

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = dbus-sharp-2.0.pc

EXTRA_DIST = \
	dbus-sharp-2.0.pc.in \
	dbus-sharp.snk \
	dbus-sharp.sln

DISTCLEANFILES = \
	dbus-sharp-2.0.pc

MAINTAINERCLEANFILES = \
	INSTALL \
	Makefile.in \
	aclocal.m4 \
	config.guess \
	config.sub \
	configure \
	install-sh \
	missing

