#!/usr/bin/env bash
# What the samples are checked with (the framework's CI and detector do not build them): restore, the format of every file, and a build with warnings as errors, for the solution of each sample.
# Running the systems is separate: each has its run-all.sh and smoke-test.sh (see README.md).
#
# Usage: samples/check.sh [abc|edx|hsx]   (no name: both)
set -euo pipefail
cd "$(dirname "$0")"

# every host project (a project of the web SDK) sets <RootNamespace> to its name without the host suffix, and no namespace of a host has the suffix (framework ADR-0158, samples ADR-0072)
check_host_namespaces() {
    local sample failed=0 project name expected folder
    sample="$(dirname "$1")"
    while IFS= read -r project; do
        name="$(basename "$project" .csproj)"
        expected="$(echo "$name" | sed -E 's/\.(Http|Wcf|Grpc)\.Host$//; s/\.Host$//')"
        if ! grep -q "<RootNamespace>$expected</RootNamespace>" "$project"; then echo "$project: <RootNamespace> must be $expected" >&2; failed=1; fi
        folder="$(dirname "$project")"
        if grep -rEn --include='*.cs' --include='*.cshtml' '^\s*(namespace|@namespace|@model|using)\s+[A-Za-z.]*\.(Http|Wcf|Grpc)?\.?Host(\.|;|\s*$)' "$folder" --exclude-dir=bin --exclude-dir=obj | grep -v 'Host\.Tests' | grep -vE 'Microsoft\.|System\.' ; then failed=1; fi
    done < <(grep -rl 'Sdk="Microsoft.NET.Sdk.Web"' "$sample" --include='*.csproj' | grep -v '/bin/\|/obj/')
    return "$failed"
}

check() {
    echo "== $1"
    check_host_namespaces "$1"
    dotnet restore "$1" --verbosity minimal
    dotnet format whitespace "$1" --verify-no-changes --no-restore
    dotnet format style "$1" --verify-no-changes --severity info --no-restore
    dotnet build "$1" --no-restore --configuration Release --verbosity minimal -p:TreatWarningsAsErrors=true
}

case "${1:-all}" in
    abc) check abc/Abc.sln ;;
    edx) check edx/Edx.sln ;;
    hsx) check hsx/Hsx.sln ;;
    all) check abc/Abc.sln; check edx/Edx.sln; check hsx/Hsx.sln ;;
    *) echo "unknown sample '$1' (abc, edx or hsx)" >&2; exit 2 ;;
esac
