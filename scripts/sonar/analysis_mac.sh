#!/usr/bin/env bash
# Local SonarQube analysis (Linux/macOS). The Windows twin is analysis_win.ps1; both do the same steps and print the same messages.
# See README.md in this folder.
#
# Usage:   SONAR_TOKEN=<token> ./scripts/sonar/analysis_mac.sh
# Optional: SONAR_HOST_URL (default http://localhost:8031), SONAR_PROJECT_KEY (default hsnsoft-base-framework)
set -euo pipefail

: "${SONAR_TOKEN:?SONAR_TOKEN is required (a user token from SonarQube: My Account > Security)}"
SONAR_HOST_URL="${SONAR_HOST_URL:-http://localhost:8031}"
SONAR_PROJECT_KEY="${SONAR_PROJECT_KEY:-hsnsoft-base-framework}"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
TEST_OUTPUT="$ROOT/TestResults"
export PATH="$PATH:$HOME/.dotnet/tools"

info() { printf '\033[34m[INFO] %s\033[0m\n' "$1"; }
bar() { info "================================================================================"; }

cd "$ROOT"

echo; bar
info "== PROJECT FOLDER => '$ROOT' START"
bar; echo

info "## CHECKING THE ENVIRONMENT ----------------------------------------------------"
if curl -fsS "$SONAR_HOST_URL/api/system/status" | grep -q '"status":"UP"'; then
    echo "SonarQube is up at $SONAR_HOST_URL"
else
    echo "SonarQube is not reachable at $SONAR_HOST_URL (start it: docker compose -f docker/sonarqube.compose.yml up -d)" >&2
    exit 1
fi
if docker info >/dev/null 2>&1; then
    echo "Docker is running (the integration tests need it)"
else
    echo "Docker is not running: the integration tests will fail" >&2
    exit 1
fi
echo

info "## SONAR SCANNER INSTALL -------------------------------------------------------"
if dotnet tool list -g | grep -q dotnet-sonarscanner; then
    echo "dotnet-sonarscanner already installed"
else
    dotnet tool install --global dotnet-sonarscanner
fi
info "## SONAR SCANNER INSTALL COMPLETED ---------------------------------------------"
echo

info "## TEST FOLDER CLEANING --------------------------------------------------------"
if [ -d "$TEST_OUTPUT" ]; then
    echo "Cleaning temporary Test Output path $TEST_OUTPUT"
    rm -rf "$TEST_OUTPUT"
fi
info "## TEST FOLDER CLEANING COMPLETED ----------------------------------------------"
echo

info "## SONAR SCANNER ANALYSIS ------------------------------------------------------"
dotnet sonarscanner begin \
    /k:"$SONAR_PROJECT_KEY" \
    /d:sonar.host.url="$SONAR_HOST_URL" \
    /d:sonar.token="$SONAR_TOKEN" \
    /d:sonar.cs.vstest.reportsPaths="$TEST_OUTPUT/**/*.trx" \
    /d:sonar.cs.opencover.reportsPaths="$TEST_OUTPUT/**/coverage.opencover.xml" \
    /d:sonar.coverage.exclusions="test/**,samples/**,scripts/**,**/Migrations/**,**/*Test*.cs,**/*Test*/**,**/tmp/**,**/Tmp/**,**/TMP/**" \
    /d:sonar.inclusions="**/*.cs,**/*.js,**/*.css" \
    /d:sonar.exclusions="test/**,samples/**,scripts/**,**/Migrations/**,**/*Test*.cs,**/*Test*/**,**/tmp/**,**/Tmp/**,**/TMP/**,**/data/**,**/logs/**,**/sample/**"
echo

info "## DOTNET RESTORE --------------------------------------------------------------"
dotnet restore --force --verbosity minimal
echo

info "## DOTNET BUILD ----------------------------------------------------------------"
dotnet build --no-restore --no-incremental --verbosity minimal -c Release
echo

info "## DOTNET TEST -----------------------------------------------------------------"
for project in "$ROOT"/test/*/*.csproj; do
    name="$(basename "$project" .csproj)"
    echo; info "## TEST [ $name ]"; echo
    dotnet test "$project" --no-build --no-restore -c Release \
        --settings "$ROOT/test/coverlet.runsettings" \
        --collect:"XPlat Code Coverage" \
        --results-directory "$TEST_OUTPUT/$name" \
        --logger "trx;LogFileName=$name.trx"
done
echo
info "## DOTNET TEST COMPLETED -------------------------------------------------------"
echo

info "## SONAR SCANNER CHECKING RESULTS ----------------------------------------------"
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
echo
info "## SONAR SCANNER ANALYSIS COMPLETED --------------------------------------------"
info "## DASHBOARD => $SONAR_HOST_URL/dashboard?id=$SONAR_PROJECT_KEY"
echo

bar
info "== PROJECT FOLDER => '$ROOT' FINISHED"
bar; echo
