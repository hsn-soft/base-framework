#!/usr/bin/env bash
# Runs a command and records how long it took, so the summary at the end of the CI run can list every step with its time.
#
# Usage: scripts/ci-detector/ci-timed.sh <timings file> <label> <command> [arguments...]
# Example: scripts/ci-detector/ci-timed.sh timings/unit.txt "Build" dotnet build --no-restore
#
# The time is written to the file as "label=seconds" whether the command passes or fails, and the exit code of the command is the exit code of this script.
set -uo pipefail

file="${1:?timings file}"
label="${2:?label}"
shift 2

mkdir -p "$(dirname "$file")"
started=$SECONDS
"$@"
code=$?
took=$((SECONDS - started))
printf '%s=%s\n' "$label" "$took" >> "$file"
printf '## %s took %dm %02ds\n' "$label" $((took / 60)) $((took % 60))
exit "$code"
