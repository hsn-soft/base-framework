#!/usr/bin/env bash
# What the samples are checked with (the framework's CI and detector do not build them): restore, the format of every file, and a build with warnings as errors, for the solution of each sample.
# Running the systems is separate: each has its run-all.sh and smoke-test.sh (see README.md).
#
# Usage: samples/check.sh [abc|edx]   (no name: both)
set -euo pipefail
cd "$(dirname "$0")"

check() {
    echo "== $1"
    dotnet restore "$1" --verbosity minimal
    dotnet format whitespace "$1" --verify-no-changes --no-restore
    dotnet format style "$1" --verify-no-changes --severity info --no-restore
    dotnet build "$1" --no-restore --configuration Release --verbosity minimal -p:TreatWarningsAsErrors=true
}

case "${1:-all}" in
    abc) check abc/Abc.sln ;;
    edx) check edx/Edx.sln ;;
    all) check abc/Abc.sln; check edx/Edx.sln ;;
    *) echo "unknown sample '$1' (abc or edx)" >&2; exit 2 ;;
esac
