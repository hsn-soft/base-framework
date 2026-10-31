#!/usr/bin/env bash
# Fails when a file or a folder of scripts/ that a workflow runs does not exist or is ignored by .gitignore. A .gitignore pattern for build output (`[Rr]elease/`) swallowed a whole folder of scripts once: the files were on the machine that wrote them, so
# everything passed there and the CI, which only has what is in git, could not find them.
#
# Usage: scripts/ci-detector/ci-check-referenced-files.sh [workflow folder]     (default: .github/workflows)
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/../.."
workflows="${1:-.github/workflows}"

missing=0
while IFS= read -r reference; do
    path="${reference%/}"
    if [ -d "$path" ]; then
        if git check-ignore -q -- "$path"; then
            echo "::error::A workflow runs '$path', which .gitignore ignores: git would not take it."
            missing=1
        fi
    elif [ -f "$path" ]; then
        if git check-ignore -q -- "$path"; then
            echo "::error::A workflow runs '$path', which .gitignore ignores: git would not take it."
            missing=1
        fi
    else
        echo "::error::A workflow runs '$path', which does not exist."
        missing=1
    fi
done < <(grep -ohE 'scripts/[A-Za-z0-9_./-]*[A-Za-z0-9_-]/?' "$workflows"/*.yml | sort -u)

[ "$missing" -eq 0 ] && echo "Every file of scripts/ that a workflow runs exists and is not ignored."
exit "$missing"
