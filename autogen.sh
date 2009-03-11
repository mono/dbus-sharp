#!/bin/sh

# Very simple autogen.sh script.
# Added by popular demand.

srcdir=${srcdir:-.}

autoreconf -i

conf_flags="--enable-maintainer-mode"
$srcdir/configure $conf_flags "$@"
