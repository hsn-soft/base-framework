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

elapsed() { printf '%dm %02ds' $(($1 / 60)) $(($1 % 60)); }

total="$(printf '%s\n' "$projects" | wc -l | tr -d ' ')"
number=0
started=$SECONDS
while IFS= read -r project; do
    number=$((number + 1))
    echo "==> $kind tests $number/$total: $project (started $(date +%H:%M:%S))"
    project_started=$SECONDS
    dotnet test "$project" "$@"
    echo "==> $kind tests $number/$total: $(basename "$project" .csproj) completed in $(elapsed $((SECONDS - project_started))) ($(date +%H:%M:%S))"
done <<< "$projects"
echo "==> $kind tests: $total project(s) completed in $(elapsed $((SECONDS - started)))"
