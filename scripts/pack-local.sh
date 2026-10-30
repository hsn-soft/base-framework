#!/usr/bin/env bash
# Packs the framework as Debug NuGet packages into a local feed folder so a microservice can reference
# them and be debugged step by step (the .pdb files are inside the packages).
#
# Usage:   ./scripts/pack-local.sh
# Options (environment variables):
#   LOCAL_FEED=<dir>   feed folder, default: ~/nuget-local-feed
#   NO_OPEN=1          do not open the folder in Finder
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
FEED="${LOCAL_FEED:-$HOME/nuget-local-feed}"
cd "$ROOT"

# The next stable version (the base of common.props and the number after the highest stable one NuGet has, scripts/versioning/next-version.py), as a local pre-release, so the local build sorts above the last release:
# 2026.10.40 on NuGet -> 2026.10.41-local.<unix time>. Without a network the number is 0 (2026.10.0-local.<unix time>), and the local build then sorts below the last release: say so.
if NEXT="$(python3 scripts/versioning/next-version.py --channel regular 2>/dev/null)"; then
    :
else
    NEXT="$(python3 scripts/versioning/next-version.py --channel regular --offline)"
    echo "NOTE: NuGet could not be asked, the local version is based on number 0: raise it by hand if the service takes the last release." >&2
fi
VERSION="$NEXT-local.$(date +%s)"

echo "==> Version: $VERSION"
echo "==> Feed:    $FEED"

mkdir -p "$FEED"

echo "==> Cleaning previous local packages"
rm -f "$FEED"/*-local.*.nupkg "$FEED"/*-local.*.snupkg
# NuGet caches every restored version under ~/.nuget/packages/<id>/<version>; drop the old local ones
for dir in "$HOME"/.nuget/packages/hsnsoft.base*/; do
    [ -d "$dir" ] || continue
    find "$dir" -maxdepth 1 -type d -name '*-local.*' -exec rm -rf {} +
done

echo "==> Restore / Build (Debug) / Pack"
dotnet restore --force --verbosity minimal --configfile NuGet.Config
dotnet build --no-restore --no-incremental --verbosity minimal --configuration Debug -p:Version="$VERSION"
dotnet pack --no-restore --no-build --verbosity minimal --configuration Debug --output "$FEED" -p:Version="$VERSION"

echo
echo "==> Packages in $FEED"
ls -lh "$FEED"/*"$VERSION"*.nupkg

echo
echo "==> In the microservice:"
echo "    1. Feed registered?  dotnet nuget add source \"$FEED\" --name hsn-local   (once; skip if it already exists)"
echo "    2. Set the version:  <PackageReference Include=\"HsnSoft.Base.Core\" Version=\"$VERSION\" />"

if ! dotnet nuget list source | grep -qF "$FEED"; then
    echo
    echo "NOTE: $FEED is not a registered NuGet source on this machine yet (see step 1)."
fi

if [ "${NO_OPEN:-0}" != "1" ] && command -v open >/dev/null 2>&1; then
    open "$FEED"
fi
