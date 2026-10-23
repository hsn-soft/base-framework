#!/usr/bin/env bash
# Fails when a package under src/ has no unit test project (test/<package>.Tests), so a new package cannot slip in untested.
# A package that has nothing to execute is listed in EXEMPT with the reason.
#
# Usage: scripts/check-test-projects.sh
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

EXEMPT=(
    HsnSoft.Base              # name holder that only references HsnSoft.Base.Core
    HsnSoft.Base.BlobStoring  # a single interface; its implementations are tested in the provider projects
)

missing=0
for project in src/*/*.csproj; do
    name="$(basename "$project" .csproj)"
    if printf '%s\n' "${EXEMPT[@]%% *}" | grep -qx "$name"; then
        continue
    fi
    if [ ! -f "test/$name.Tests/$name.Tests.csproj" ]; then
        echo "::error::$name has no unit test project (test/$name.Tests)."
        missing=1
    fi
done

[ "$missing" -eq 0 ] && echo "Every package has a unit test project."
exit "$missing"
