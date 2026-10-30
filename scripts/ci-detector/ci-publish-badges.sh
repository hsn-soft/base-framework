#!/usr/bin/env bash
# Publishes the endpoint files of the README badges (the .json files that ci-summary.py --badges wrote) to the branch "badges" of the repository, where shields.io reads them.
# The branch holds nothing but those files and is made (as an orphan, with no history of the code) the first time.
#
# Usage: scripts/ci-detector/ci-publish-badges.sh <folder with the .json files>
# Run by the summary job of the CI on dev (a scheduled or a manual run), with the token of the run allowed to write (permissions: contents: write).
set -euo pipefail

source_dir="$(cd "${1:?folder with the badge files}" && pwd)"
branch="badges"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

# on GitHub the token of the run (GITHUB_TOKEN, which the log hides) is in the address of the remote; on a machine without it the remote of this checkout is used
if [ -n "${GITHUB_TOKEN:-}" ] && [ -n "${GITHUB_REPOSITORY:-}" ]; then
    remote="https://x-access-token:${GITHUB_TOKEN}@github.com/${GITHUB_REPOSITORY}.git"
else
    remote="$(git remote get-url origin)"
fi

if git ls-remote --exit-code --heads "$remote" "$branch" >/dev/null 2>&1; then
    git clone --quiet --depth 1 --branch "$branch" "$remote" "$work"
else
    git clone --quiet --depth 1 "$remote" "$work"
    git -C "$work" switch --quiet --orphan "$branch"
    git -C "$work" rm -rfq . 2>/dev/null || true
fi

find "$work" -maxdepth 1 -name '*.json' -delete
cp "$source_dir"/*.json "$work"/

git -C "$work" config user.name "github-actions[bot]"
git -C "$work" config user.email "41898282+github-actions[bot]@users.noreply.github.com"
git -C "$work" add -A

if git -C "$work" diff --cached --quiet; then
    echo "The badges are what they were: nothing to publish."
    exit 0
fi

git -C "$work" commit --quiet -m "badges: $(cat "$source_dir/checked.json" | sed -n 's/.*"message": "\([0-9]*\)".*/\1/p') (UTC)"
git -C "$work" push --quiet origin "HEAD:refs/heads/$branch"
echo "The badges were published to the branch $branch."
