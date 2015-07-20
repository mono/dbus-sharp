m4_define([api_version], [2.0])
m4_define([lib_version], [0.9])

AC_PREREQ(2.13)

AC_INIT([dbus-sharp], lib_version)
AC_CONFIG_SRCDIR([src/DBus.cs])
AM_INIT_AUTOMAKE

AM_MAINTAINER_MODE

AC_SUBST([API_VERSION], [api_version])
AC_SUBST([VERSION], [lib_version])

AC_PROG_INSTALL

AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
	AC_MSG_ERROR([You need to install pkg-config])
fi
AC_SUBST(PKG_CONFIG)

MONO_REQ_VERSION=1.1.13
PKG_CHECK_MODULES(MONO, mono >= $MONO_REQ_VERSION)

AC_PATH_PROG(MCS, mcs, no)
if test "x$MCS" = "xno"; then
	AC_MSG_ERROR([You need to install mcs])
fi
AC_SUBST(MCS)

AC_PATH_PROG(GACUTIL, gacutil, no)
if test "x$GACUTIL" = "xno"; then
	AC_MSG_ERROR([You need to install gacutil])
fi
AC_SUBST(GACUTIL)

AC_PATH_PROG(XBUILD, xbuild, no)
if test "x$GACUTIL" = "xno"; then
	AC_MSG_ERROR([You need to install xbuild])
fi
AC_SUBST(XBUILD)

required_assemblies="Mono.Posix"

for asm in $required_assemblies; do
	AC_MSG_CHECKING([for $asm.dll])
	if test -n "`$GACUTIL /l $asm.dll | grep ^$asm.dll`"; then
		AC_MSG_RESULT([not found])
		AC_MSG_ERROR([missing required assembly in the GAC: $asm.dll])
	else
		AC_MSG_RESULT([found])
	fi
done

NUNIT_REQUIRED=2.4.7

AC_ARG_ENABLE(tests, AC_HELP_STRING([--enable-tests], [Enable NUnit tests]),
	enable_tests=$enableval, enable_tests="no")

if test "x$enable_tests" = "xno"; then
	do_tests=no
	AM_CONDITIONAL(ENABLE_TESTS, false)
else
	PKG_CHECK_MODULES(NUNIT, nunit >= $NUNIT_REQUIRED,
		do_tests="yes", do_tests="no")

	AC_SUBST(NUNIT_LIBS)
	AM_CONDITIONAL(ENABLE_TESTS, test "x$do_tests" = "xyes")

	if test "x$do_tests" = "xno"; then
		PKG_CHECK_MODULES(NUNIT, mono-nunit >= 2.4,
			do_tests="yes", do_tests="no")

		AC_SUBST(NUNIT_LIBS)
		AM_CONDITIONAL(ENABLE_TESTS, test "x$do_tests" = "xyes")

		if test "x$do_tests" = "xno"; then
			AC_MSG_WARN([Could not find nunit: tests will not be available])
		fi
	fi
fi

AC_OUTPUT([
Makefile
dbus-sharp-2.0.pc
src/AssemblyInfo.cs
src/Makefile
tools/Makefile
examples/Makefile
tests/Makefile
])

