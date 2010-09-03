AC_INIT(src/DBus.cs)

AC_PREREQ(2.13)

API_VERSION=1.0
#VERSION=$API_VERSION.0
VERSION=0.6.1

AC_SUBST(API_VERSION)
AC_SUBST(VERSION)

AM_INIT_AUTOMAKE(ndesk-dbus, $VERSION)

AM_MAINTAINER_MODE

AC_PROG_INSTALL

AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
	AC_MSG_ERROR([You need to install pkg-config])
fi
AC_SUBST(PKG_CONFIG)

MONO_REQ_VERSION=1.1.13
PKG_CHECK_MODULES(MONO, mono >= $MONO_REQ_VERSION)

AC_PATH_PROG(GMCS, gmcs, no)
if test "x$GMCS" = "xno"; then
	AC_MSG_ERROR([You need to install gmcs])
fi
AC_SUBST(GMCS)

AC_PATH_PROG(GACUTIL, gacutil, no)
if test "x$GACUTIL" = "xno"; then
	AC_MSG_ERROR([You need to install gacutil])
fi
AC_SUBST(GACUTIL)

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

AC_OUTPUT([
Makefile
ndesk-dbus-1.0.pc
src/AssemblyInfo.cs
src/Makefile
tools/Makefile
examples/Makefile
])

