#!/usr/bin/env bash
# Deletes build output, test results and editor/OS leftovers (Linux/macOS). The Windows twin is cleaner_win.ps1; both do the same
# steps and print the same messages. See README.md in this folder.
#
# Usage: ./scripts/cleaner/cleaner_mac.sh [--dry-run]
#   --dry-run   only list what would be deleted
set -euo pipefail

DRY_RUN=false
case "${1:-}" in
    "") ;;
    --dry-run) DRY_RUN=true ;;
    *) echo "Usage: $0 [--dry-run]" >&2; exit 2 ;;
esac

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

# What is considered garbage. Names, matched anywhere under the repository (never inside .git).
DIRECTORIES=(bin obj TestResults .sonarqube .vs)
FILES=(.DS_Store Thumbs.db '*.user' '*.suo' '*.trx' coverage.cobertura.xml coverage.opencover.xml)

info() { printf '\033[34m[INFO] %s\033[0m\n' "$1"; }
bar() { info "================================================================================"; }

cd "$ROOT"

echo; bar
info "== PROJECT FOLDER => '$ROOT' START"
bar; echo

info "## SEARCHING -------------------------------------------------------------------"
dir_expr=()
for name in "${DIRECTORIES[@]}"; do dir_expr+=(-o -name "$name"); done
file_expr=()
for name in "${FILES[@]}"; do file_expr+=(-o -name "$name"); done

targets=()
while IFS= read -r path; do
    [ -n "$path" ] && targets+=("$path")
done < <(
    {
        find . -name .git -prune -o -type d \( -false "${dir_expr[@]}" \) -print -prune
        find . -name .git -prune -o \( -type d \( -false "${dir_expr[@]}" \) \) -prune -o -type f \( -false "${file_expr[@]}" \) -print
    } | sort
)

kept=0
selected=()
for path in "${targets[@]}"; do
    # never delete something that is committed
    if [ -n "$(git ls-files -- "$path" 2>/dev/null | head -n 1)" ]; then
        echo "Keeping '$path' (it contains tracked files)"
        kept=$((kept + 1))
    else
        selected+=("$path")
    fi
done
echo "${#selected[@]} item(s) to delete, $kept kept"
info "## SEARCHING COMPLETED ---------------------------------------------------------"
echo

if [ "$DRY_RUN" = true ]; then
    info "## DRY RUN: NOTHING IS DELETED -------------------------------------------------"
    for path in "${selected[@]}"; do echo "would delete $path"; done
else
    info "## DELETING --------------------------------------------------------------------"
    for path in "${selected[@]}"; do
        rm -rf -- "$path"
        echo "deleted $path"
    done
    info "## DELETING COMPLETED ----------------------------------------------------------"
fi
echo

bar
info "== PROJECT FOLDER => '$ROOT' FINISHED"
bar; echo
