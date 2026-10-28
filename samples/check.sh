#!/usr/bin/env bash
# What the samples are checked with (the framework's CI and detector do not build them): restore, the format of every file, and a build with warnings as errors.
# Running the systems is separate: each has its run-all.sh and smoke-test.sh (see README.md).
#
# Usage: samples/check.sh
set -euo pipefail
cd "$(dirname "$0")"

dotnet restore Samples.sln --verbosity minimal
dotnet format whitespace Samples.sln --verify-no-changes --no-restore
dotnet format style Samples.sln --verify-no-changes --severity info --no-restore
dotnet build Samples.sln --no-restore --configuration Release --verbosity minimal -p:TreatWarningsAsErrors=true
