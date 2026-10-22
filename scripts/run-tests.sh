#!/usr/bin/env bash
# Runs one kind of test projects, chosen by project name (no test traits to forget):
#   unit          test/**/*.Tests.csproj             fast, no external services
#   integration   test/**/*.IntegrationTests.csproj  real services (Docker / Testcontainers)
#
# Usage: scripts/run-tests.sh <unit|integration> [dotnet test arguments...]
# Example: scripts/run-tests.sh unit --configuration Release --no-build
set -euo pipefail

kind="${1:-}"
[ "$#" -gt 0 ] && shift

case "$kind" in
    unit) pattern='*.Tests.csproj' ;;
    integration) pattern='*.IntegrationTests.csproj' ;;
    *) echo "Usage: $0 <unit|integration> [dotnet test arguments...]" >&2; exit 2 ;;
esac

cd "$(dirname "${BASH_SOURCE[0]}")/.."

projects="$(find test -name "$pattern" -not -path '*/obj/*' -not -path '*/bin/*' 2>/dev/null | sort)"

if [ -z "$projects" ]; then
    echo "No $kind test projects found ($pattern under test/)."
    # Having no unit tests is a configuration mistake; having no integration tests is fine.
    [ "$kind" = "unit" ] && exit 1
    exit 0
fi

while IFS= read -r project; do
    echo "==> $kind tests: $project"
    dotnet test "$project" "$@"
done <<< "$projects"
