#!/usr/bin/env bash
# Pre-PR check (Linux/macOS): runs on your machine what the CI will run on the pull request, and shows one table of what passed and
# where it would break. The Windows twin is ci_detector_win.ps1; both run the same stages and print the same tables.
# See README.md in this folder.
#
# Usage: ./scripts/ci-detector/ci_detector_mac.sh [--skip-integration]
#   --skip-integration   do not run the integration tests (no Docker needed); the coverage gate is then skipped too
#
# Keep it in step with .github/workflows/ci-control-workflow.yml: when a step is added there, add the stage here.
set -uo pipefail

SKIP_INTEGRATION=false
case "${1:-}" in
    "") ;;
    --skip-integration) SKIP_INTEGRATION=true ;;
    *) echo "Usage: $0 [--skip-integration]" >&2; exit 2 ;;
esac

# dotnet prints its messages in the language of the machine; the detector reads some of them, so ask for English
export DOTNET_CLI_UI_LANGUAGE=en VSLANG=1033

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OUT="$ROOT/TestResults/detector"
LOGS="$OUT/logs"
cd "$ROOT"

if [ -t 1 ]; then
    BLUE=$'\033[34m'; GREEN=$'\033[32m'; RED=$'\033[31m'; YELLOW=$'\033[33m'; BOLD=$'\033[1m'; RESET=$'\033[0m'
else
    BLUE=''; GREEN=''; RED=''; YELLOW=''; BOLD=''; RESET=''
fi
info() { printf '%s[INFO] %s%s\n' "$BLUE" "$1" "$RESET"; }
bar() { info "================================================================================"; }
elapsed() { printf '%dm %02ds' $(($1 / 60)) $(($1 % 60)); }
DETECTOR_STARTED=$SECONDS

THRESHOLD="$(sed -n "s/.*COVERAGE_THRESHOLD: '\([0-9.]*\)'.*/\1/p" .github/workflows/ci-control-workflow.yml | head -n 1)"
THRESHOLD="${THRESHOLD:-99}"

# a clean screen for the run (only on a terminal, not when the output goes to a file)
if [ -t 1 ]; then clear; fi

echo; bar
info "== DETECTOR => '$ROOT' START ($(date +%H:%M:%S))"
bar; echo

info "## CHECKING THE TOOLS ----------------------------------------------------------"
missing=false
for tool in dotnet git python3; do
    if command -v "$tool" >/dev/null 2>&1; then echo "$tool found"; else echo "$tool NOT found" >&2; missing=true; fi
done
if [ "$SKIP_INTEGRATION" = false ]; then
    if docker info >/dev/null 2>&1; then echo "docker is running"; else echo "docker is NOT running (needed by the integration tests; or use --skip-integration)" >&2; missing=true; fi
fi
[ "$missing" = true ] && { echo "Fix the above and run again." >&2; exit 2; }
echo

# the CI starts from a clean checkout: so does the detector. The cleaning script lists what it deletes; that list goes to the log, the screen gets its summary.
info "## CLEANING (build output, old test results, editor leftovers) -----------------"
clean_started=$SECONDS
clean_log="$(mktemp)"
if ! "$ROOT/scripts/cleaner/cleaner_mac.sh" > "$clean_log" 2>&1; then
    cat "$clean_log" >&2
    echo "The cleaning failed; fix the above and run again." >&2
    exit 2
fi
CLEAN_TOOK=$((SECONDS - clean_started))
sed 's/\x1b\[[0-9;]*m//g' "$clean_log" | grep -E 'item\(s\) to delete' | sed 's/^/  /'
echo

rm -rf "$OUT"; mkdir -p "$LOGS"
mv "$clean_log" "$LOGS/clean.log"

NAMES=(); RESULTS=(); TIMES=(); DETAILS=()
record() { NAMES+=("$1"); RESULTS+=("$2"); TIMES+=("$3"); DETAILS+=("$4"); }
record "Clean" PASS "${CLEAN_TOOK}s" ""

# stage <id> <title> <command...>: runs it, keeps its output in logs/<id>.log, records PASS/FAIL
stage() {
    local id="$1" title="$2"; shift 2
    # a stage that shows its progress (LIVE=1) says that it is running on a line of its own; the result comes on a line of its own after the progress
    if [ "${LIVE:-}" = 1 ]; then printf '  %-36s running\n' "$title"; else printf '  %-36s ' "$title"; fi
    local started=$SECONDS
    if "$@" > "$LOGS/$id.log" 2>&1; then
        local took=$((SECONDS - started))
        [ "${LIVE:-}" = 1 ] && printf '  %-36s ' "$title"
        printf '%sPASS%s  (%ss)\n' "$GREEN" "$RESET" "$took"
        record "$title" PASS "${took}s" ""
        return 0
    fi
    local took=$((SECONDS - started))
    [ "${LIVE:-}" = 1 ] && printf '  %-36s ' "$title"
    printf '%sFAIL%s  (%ss)\n' "$RED" "$RESET" "$took"
    record "$title" FAIL "${took}s" "TestResults/detector/logs/$id.log"
    return 1
}
skip() { printf '  %-36s %sSKIPPED%s  (%s)\n' "$2" "$YELLOW" "$RESET" "$3"; record "$2" SKIPPED "-" "$3"; }

format_cs() {
    dotnet format whitespace HsnSoft.Base.sln --verify-no-changes --no-restore &&
        dotnet format style HsnSoft.Base.sln --verify-no-changes --severity info --no-restore
}
# The output of a stage goes to its log; what is shown on the screen (fd 3, the terminal) is only the progress: a line for every test project that
# finished, a line for every test that passed (with its time: the integration tests, which run for minutes) and every test that failed.
exec 3>&1
progress() {
    local line passed=0 shown
    while IFS= read -r line; do
        case "$line" in
            "==> "*" completed in "*) printf '      %s\n' "${line#==> }" >&3 ;;
            "  Failed "*"]") printf '      %s\n' "$(printf '%s' "$line" | sed 's/^ *//')" >&3 ;;
            "  Passed "*"]")
                passed=$((passed + 1))
                shown="${line#  Passed }"
                shown="${shown#HsnSoft.Base.IntegrationTests.}"
                printf '      %3d  %s\n' "$passed" "$(printf '%.110s' "$shown")" >&3 ;;
        esac
    done
}
run_unit() {
    local code
    ./scripts/run-tests.sh unit --no-build --configuration Release --verbosity minimal \
        --settings test/coverlet.runsettings --collect:"XPlat Code Coverage" --logger trx --results-directory "$OUT/unit" 2>&1 | tee >(progress)
    code=${PIPESTATUS[0]}
    sleep 0.3
    return "$code"
}
run_integration() {
    local code
    ./scripts/run-tests.sh integration --no-build --configuration Release --verbosity minimal \
        --settings test/coverlet.runsettings --collect:"XPlat Code Coverage" --logger trx --logger "console;verbosity=normal" --results-directory "$OUT/integration" 2>&1 | tee >(progress)
    code=${PIPESTATUS[0]}
    sleep 0.3
    return "$code"
}
reports() { find "$OUT/$1" -name coverage.cobertura.xml -not -path '*/In/*' 2>/dev/null; }
gate() { python3 scripts/ci-detector/ci-check-coverage.py "$THRESHOLD" $(reports unit) $(reports integration); }

info "## STAGES ----------------------------------------------------------------------"
stage restore "Restore" dotnet restore HsnSoft.Base.sln --verbosity minimal && RESTORED=true || RESTORED=false

stage test-projects "Every package has a test project" ./scripts/ci-detector/ci-check-test-project-exist.sh
stage csproj-order "Project file reference order" python3 scripts/ci-detector/ci-force-csproj-order.py
stage framework-references "Framework project references" python3 scripts/ci-detector/ci-force-framework-references-rules.py
if [ "$RESTORED" = true ]; then
    stage format-cs "Format (C#)" format_cs
else
    skip format-cs "Format (C#)" "restore failed"
fi
stage format-json "Format (JSON)" dotnet run scripts/ci-detector/format-json.cs -- --all --check

BUILT=false
if [ "$RESTORED" = true ]; then
    stage build "Build (Release, warnings are errors)" dotnet build HsnSoft.Base.sln --no-restore --no-incremental \
        --configuration Release --verbosity minimal -p:TreatWarningsAsErrors=true && BUILT=true
else
    skip build "Build (Release, warnings are errors)" "restore failed"
fi

UNIT_OK=false; INTEGRATION_OK=false
if [ "$BUILT" = true ]; then
    LIVE=1 stage unit "Unit tests" run_unit && UNIT_OK=true
    if [ "$SKIP_INTEGRATION" = true ]; then
        skip integration "Integration tests" "--skip-integration"
    else
        LIVE=1 stage integration "Integration tests" run_integration && INTEGRATION_OK=true
    fi
else
    skip unit "Unit tests" "build did not pass"
    skip integration "Integration tests" "build did not pass"
fi

if [ "$UNIT_OK" = true ] && [ "$INTEGRATION_OK" = true ]; then
    stage coverage "Coverage gate (>= ${THRESHOLD} %)" gate
elif [ "$SKIP_INTEGRATION" = true ]; then
    skip coverage "Coverage gate (>= ${THRESHOLD} %)" "needs the integration tests"
else
    skip coverage "Coverage gate (>= ${THRESHOLD} %)" "tests did not pass"
fi
echo

# ---- the table ------------------------------------------------------------------------------------------------------------
failed=0; skipped=0
for i in "${!RESULTS[@]}"; do
    [ "${RESULTS[$i]}" = FAIL ] && failed=$((failed + 1))
    [ "${RESULTS[$i]}" = SKIPPED ] && skipped=$((skipped + 1))
done

info "## RESULT ----------------------------------------------------------------------"
printf '  %-38s %-9s %-7s %s\n' "Stage" "Result" "Time" "Detail"
printf '  %-38s %-9s %-7s %s\n' "--------------------------------------" "---------" "-------" "------"
for i in "${!NAMES[@]}"; do
    case "${RESULTS[$i]}" in PASS) color=$GREEN ;; FAIL) color=$RED ;; *) color=$YELLOW ;; esac
    printf '  %-38s %s%-9s%s %-7s %s\n' "${NAMES[$i]}" "$color" "${RESULTS[$i]}" "$RESET" "${TIMES[$i]}" "${DETAILS[$i]}"
done
printf '  %-38s %-9s %s\n' "Total" "" "$(elapsed $((SECONDS - DETECTOR_STARTED)))"
echo

# totals from the .trx result files, which are the same in every language
counts() { # <unit|integration> -> "total passed failed skipped"
    python3 - "$OUT/$1" <<'PY'
import glob, sys
import xml.etree.ElementTree as ET
total = passed = failed = skipped = 0
for path in glob.glob(sys.argv[1] + "/**/*.trx", recursive=True):
    root = ET.parse(path).getroot()
    counters = root.find(".//{*}ResultSummary/{*}Counters")
    total += int(counters.get("total")); passed += int(counters.get("passed"))
    failed += int(counters.get("failed")); skipped += int(counters.get("notExecuted"))
print(total, passed, failed, skipped)
PY
}
coverage_of() { python3 scripts/ci-detector/ci-check-coverage.py 0 $(reports "$1") 2>/dev/null | sed -n 's/^Line coverage: \(.*\) (threshold.*/\1/p'; }

if [ -f "$LOGS/unit.log" ]; then
    info "## TESTS ----------------------------------------------------------------------"
    printf '  %-20s %7s %7s %7s %7s\n' "" total passed failed skipped
    for kind in unit integration; do
        [ -f "$LOGS/$kind.log" ] || continue
        read -r total passed failed_tests skipped_tests <<< "$(counts "$kind")"
        printf '  %-20s %7s %7s %7s %7s\n' "$kind" "$total" "$passed" "$failed_tests" "$skipped_tests"
    done
    echo
fi
if [ "$UNIT_OK" = true ]; then
    info "## LINE COVERAGE (src) ---------------------------------------------------------"
    echo "  unit tests only        $(coverage_of unit)"
    [ "$INTEGRATION_OK" = true ] && echo "  integration tests only $(coverage_of integration)"
    if [ "$INTEGRATION_OK" = true ]; then
        merged="$(python3 scripts/ci-detector/ci-check-coverage.py 0 $(reports unit) $(reports integration) | sed -n 's/^Line coverage: \(.*\) (threshold.*/\1/p')"
        echo "  merged (what CI gates) $merged   threshold ${THRESHOLD} %"
    fi
    echo
fi

for i in "${!RESULTS[@]}"; do
    if [ "${RESULTS[$i]}" = FAIL ]; then
        info "## ${NAMES[$i]}: last lines of ${DETAILS[$i]}"
        # the lines that say what broke (errors, failed tests and their expected/actual values); the end of the log if there are none
        clean="$(sed 's/\x1b\[[0-9;]*m//g' "$ROOT/${DETAILS[$i]}" | grep -v '^\s*$')"
        picked="$(printf '%s\n' "$clean" | grep -E ' error |^ *Failed [A-Za-z]|Expected:|Actual:|Error Message|Assert\.|Unable to fix' | awk '!seen[$0]++' | head -n 12)"
        printf '%s\n' "${picked:-$(printf '%s\n' "$clean" | tail -n 12)}" | cut -c1-200 | sed 's/^/    /'
        echo
    fi
done

if [ "$failed" -gt 0 ]; then
    printf '%s%s%d stage(s) would fail in the CI. Fix them and run the detector again before you open the pull request.%s\n' "$BOLD" "$RED" "$failed" "$RESET"
    code=1
elif [ "$skipped" -gt 0 ]; then
    printf '%s%sNothing failed, but %d stage(s) were skipped: this is not the full check. Run it without --skip-integration before you open the pull request.%s\n' "$BOLD" "$YELLOW" "$skipped" "$RESET"
    code=0
else
    printf '%s%sEvery stage passed. The pull request is safe to open.%s\n' "$BOLD" "$GREEN" "$RESET"
    code=0
fi
echo

bar
info "== DETECTOR FINISHED ($(date +%H:%M:%S), total $(elapsed $((SECONDS - DETECTOR_STARTED))))"
bar; echo
exit "$code"
