#!/usr/bin/env bash
# Local SonarQube analysis (Linux/macOS). The Windows twin is analysis_win.ps1; both do the same steps and print the same messages.
# See README.md in this folder.
#
# Usage:   ./scripts/sonar/analysis_mac.sh      (SONAR_TOKEN comes from scripts/sonar/.env, or from the environment)
# Optional: SONAR_HOST_URL (default http://localhost:8031), SONAR_PROJECT_KEY (default hsnsoft-base-framework)
set -euo pipefail

# Settings from the git-ignored .env next to this script; a variable already set in the environment wins.
ENV_FILE="$(dirname "${BASH_SOURCE[0]}")/.env"
if [ -f "$ENV_FILE" ]; then
    while IFS='=' read -r key value || [ -n "$key" ]; do
        case "$key" in ''|\#*) continue ;; esac
        value="${value%\"}"; value="${value#\"}"
        [ -z "${!key:-}" ] && export "$key=$value"
    done < "$ENV_FILE"
fi

: "${SONAR_TOKEN:?SONAR_TOKEN is required (put it in scripts/sonar/.env, see .env.example; a user token from SonarQube: My Account > Security)}"
SONAR_HOST_URL="${SONAR_HOST_URL:-http://localhost:8031}"
SONAR_PROJECT_KEY="${SONAR_PROJECT_KEY:-hsnsoft-base-framework}"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
TEST_OUTPUT="$ROOT/TestResults"
export PATH="$PATH:$HOME/.dotnet/tools"

info() { printf '\033[34m[INFO] %s\033[0m\n' "$1"; }
elapsed() { printf '%dm %02ds' $(($1 / 60)) $(($1 % 60)); }
STARTED_AT=$SECONDS
bar() { info "================================================================================"; }

cd "$ROOT"

# a clean screen for the run (only on a terminal, not when the output goes to a file)
if [ -t 1 ]; then clear; fi

echo; bar
info "== PROJECT FOLDER => '$ROOT' START ($(date +%H:%M:%S))"
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

info "## SONAR SCANNER ANALYSIS STARTED ($(date +%H:%M:%S)) -----------------------------"
dotnet sonarscanner begin \
    /k:"$SONAR_PROJECT_KEY" \
    /d:sonar.host.url="$SONAR_HOST_URL" \
    /d:sonar.token="$SONAR_TOKEN" \
    /d:sonar.cs.vstest.reportsPaths="$TEST_OUTPUT/**/*.trx" \
    /d:sonar.cs.opencover.reportsPaths="$TEST_OUTPUT/**/coverage.opencover.xml" \
    /d:sonar.coverage.exclusions="test/**,scripts/**,**/Migrations/**,**/*Test*.cs,**/*Test*/**,**/tmp/**,**/Tmp/**,**/TMP/**" \
    /d:sonar.inclusions="src/**/*.cs,src/**/*.js,src/**/*.css" \
    /d:sonar.exclusions="test/**,scripts/**,**/Migrations/**,**/*Test*.cs,**/*Test*/**,**/tmp/**,**/Tmp/**,**/TMP/**,**/data/**,**/logs/**,**/sample/**"
echo

info "## DOTNET RESTORE --------------------------------------------------------------"
dotnet restore --force --verbosity minimal
echo

info "## DOTNET BUILD ----------------------------------------------------------------"
dotnet build --no-restore --no-incremental --verbosity minimal -c Release -p:TreatWarningsAsErrors=true
echo

info "## DOTNET TEST -----------------------------------------------------------------"
# the unit tests first, the integration tests (Docker, the slow ones) last: a broken unit test is found at once, not after the containers
projects=()
integration=()
for project in "$ROOT"/test/*/*.csproj; do
    case "$project" in
        *IntegrationTests.csproj) integration+=("$project") ;;
        *) projects+=("$project") ;;
    esac
done
projects+=("${integration[@]}")
total=${#projects[@]}
number=0
TESTS_STARTED_AT=$SECONDS
for project in "${projects[@]}"; do
    number=$((number + 1))
    name="$(basename "$project" .csproj)"
    echo; info "## TEST $number/$total [ $name ] STARTED ($(date +%H:%M:%S))"; echo
    project_started_at=$SECONDS
    dotnet test "$project" --no-build --no-restore -c Release \
        --settings "$ROOT/test/coverlet.runsettings" \
        --collect:"XPlat Code Coverage" \
        --results-directory "$TEST_OUTPUT/$name" \
        --logger "trx;LogFileName=$name.trx" \
        --logger "console;verbosity=normal"
    info "## TEST $number/$total [ $name ] COMPLETED in $(elapsed $((SECONDS - project_started_at)))"
done
echo
info "## DOTNET TEST COMPLETED in $(elapsed $((SECONDS - TESTS_STARTED_AT))) ---------------------------------------"
echo

info "## SONAR SCANNER CHECKING RESULTS ----------------------------------------------"
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
echo
info "## SONAR SCANNER ANALYSIS COMPLETED ($(date +%H:%M:%S), $(elapsed $((SECONDS - STARTED_AT))) since the start) ---------"
info "## DASHBOARD => $SONAR_HOST_URL/dashboard?id=$SONAR_PROJECT_KEY"
echo

info "## SONAR OVERVIEW --------------------------------------------------------------"
# SonarQube processes the uploaded report in the background: wait for that task, then read the project's measures.
CE_TASK_URL="$(sed -n 's/^ceTaskUrl=//p' "$ROOT/.sonarqube/out/.sonar/report-task.txt")"
SONAR_HOST_URL="$SONAR_HOST_URL" SONAR_TOKEN="$SONAR_TOKEN" SONAR_PROJECT_KEY="$SONAR_PROJECT_KEY" CE_TASK_URL="$CE_TASK_URL" python3 - <<'PY'
import base64, json, os, sys, time, urllib.parse, urllib.request

host, project, task_url = os.environ["SONAR_HOST_URL"], os.environ["SONAR_PROJECT_KEY"], os.environ["CE_TASK_URL"]
auth = "Basic " + base64.b64encode((os.environ["SONAR_TOKEN"] + ":").encode()).decode()


def get(url):
    return json.load(urllib.request.urlopen(urllib.request.Request(url, headers={"Authorization": auth}), timeout=30))


status = "PENDING"
for _ in range(60):
    status = get(task_url)["task"]["status"]
    if status not in ("PENDING", "IN_PROGRESS"):
        break
    time.sleep(2)
if status != "SUCCESS":
    print(f"SonarQube did not finish processing the report (status {status}); the numbers are not shown.")
    sys.exit(0)

keys = ["alert_status", "software_quality_security_rating", "software_quality_reliability_rating",
        "software_quality_maintainability_rating", "security_hotspots_reviewed", "security_review_rating",
        "coverage", "line_coverage", "tests", "test_failures", "duplicated_lines_density", "ncloc",
        "software_quality_security_issues", "software_quality_reliability_issues", "software_quality_maintainability_issues"]
query = urllib.parse.urlencode({"component": project, "metricKeys": ",".join(keys)})
m = {x["metric"]: x.get("value", "") for x in get(f"{host}/api/measures/component?{query}")["component"]["measures"]}
rating = lambda key: "ABCDE"[int(float(m[key])) - 1] if key in m else "-"
percent = lambda key: f"{float(m[key]):.1f} %" if key in m else "-"
value = lambda key: m.get(key, "-")

rows = [
    ("Quality Gate", "Passed" if value("alert_status") == "OK" else "FAILED"),
    ("Security", f"{rating('software_quality_security_rating')}   ({value('software_quality_security_issues')} open issues)"),
    ("Reliability", f"{rating('software_quality_reliability_rating')}   ({value('software_quality_reliability_issues')} open issues)"),
    ("Maintainability", f"{rating('software_quality_maintainability_rating')}   ({value('software_quality_maintainability_issues')} open issues)"),
    ("Security hotspots", f"{rating('security_review_rating')}   ({percent('security_hotspots_reviewed')} reviewed)"),
    ("Coverage (lines + branches)", percent("coverage")),
    ("Line coverage", percent("line_coverage")),
    ("Tests", f"{value('tests')}   ({value('test_failures')} failed)"),
    ("Duplications", percent("duplicated_lines_density")),
    ("Lines of code", value("ncloc")),
]
width = max(len(name) for name, _ in rows)
for name, text in rows:
    print(f"  {name.ljust(width)}  {text}")
PY
info "## SONAR OVERVIEW COMPLETED ----------------------------------------------------"
echo

bar
info "== PROJECT FOLDER => '$ROOT' FINISHED ($(date +%H:%M:%S), total $(elapsed $((SECONDS - STARTED_AT))))"
bar; echo
