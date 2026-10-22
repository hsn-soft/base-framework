#!/usr/bin/env bash
# Local SonarQube analysis. Usage: SONAR_TOKEN=<token> ./analysis_sonar.sh
set -euo pipefail
: "${SONAR_TOKEN:?SONAR_TOKEN is required}"
SONAR_HOST_URL="${SONAR_HOST_URL:-http://localhost:8031}"
ROOT="$(cd "$(dirname "$0")" && pwd)"
export PATH="$PATH:$HOME/.dotnet/tools"

dotnet tool list -g | grep -q dotnet-sonarscanner || dotnet tool install --global dotnet-sonarscanner
rm -rf "$ROOT/TestResults"

dotnet sonarscanner begin \
    /k:"hsnsoft-base-framework" \
    /d:sonar.host.url="$SONAR_HOST_URL" \
    /d:sonar.token="$SONAR_TOKEN" \
    /d:sonar.cs.vstest.reportsPaths="$ROOT/TestResults/**/*.trx" \
    /d:sonar.cs.opencover.reportsPaths="$ROOT/TestResults/**/coverage.opencover.xml" \
    /d:sonar.exclusions="test/**,**/Migrations/**,**/*Test*.cs,**/*Test*/**,**/tmp/**,**/Tmp/**,**/TMP/**,**/data/**,**/logs/**,**/sample/**" \
    /d:sonar.coverage.exclusions="test/**,**/Migrations/**,**/*Test*.cs,**/*Test*/**,**/tmp/**,**/Tmp/**,**/TMP/**"

dotnet restore --force --verbosity minimal
dotnet build --no-restore --no-incremental --verbosity minimal -c Release

for proj in "$ROOT"/test/*/*.csproj; do
    name="$(basename "$proj" .csproj)"
    dotnet test "$proj" --no-build --no-restore -c Release \
        --settings "$ROOT/test/coverlet.runsettings" \
        --collect:"XPlat Code Coverage" \
        --results-directory "$ROOT/TestResults/$name" \
        --logger "trx;LogFileName=$name.trx"
done

dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
