#!/usr/bin/env python3
"""The version of the packages a release is going to publish (ADR-0115).

The base of the version (year.month, as the owner of the repository wrote it in common.props) is fixed by hand; the number after it is the next one after the highest
**stable** version of that base that NuGet has, for any package of the repository, so a number that exists anywhere is never made again:

    regular    2026.10.41
    candidate  2026.10.41-rc.261031.57      (the pre-release of the NEXT stable version: 2026.10.41-rc... sorts after 2026.10.40 and before 2026.10.41)

Usage: next-version.py --channel <candidate|regular> [--run <number>] [--date <yyMMdd>] [--base <year.month>] [--source <url>] [--offline]
  --base     defaults to <BaseVersion> of common.props
  --run      the number of the run of the workflow (candidate only; always growing, so the candidates sort in the order they were made)
  --date     defaults to today, UTC (candidate only)
  --offline  do not ask NuGet: the number is 0 (a build on a machine without a network, or a month that has no release yet)

Prints the version; on GitHub Actions also writes `version`, `previous_tag` (the tag of the last stable version of the base, empty when there is none) and `is_prerelease` to GITHUB_OUTPUT.
"""
import argparse
import datetime
import glob
import json
import os
import re
import sys
import urllib.error
import urllib.request

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
FLAT_CONTAINER = "https://api.nuget.org/v3-flatcontainer"


def base_version_of_the_repository():
    text = open(os.path.join(ROOT, "common.props"), encoding="utf-8").read()
    match = re.search(r"<BaseVersion>([^<]+)</BaseVersion>", text)
    if not match:
        raise SystemExit("BaseVersion was not found in common.props")
    return match.group(1).strip()


def package_ids():
    """The ids of the packages of the repository: the PackageId of every project of src/ (the name of the project when it says none)."""
    ids = []
    for project in sorted(glob.glob(os.path.join(ROOT, "src", "*", "*.csproj"))):
        text = open(project, encoding="utf-8").read()
        match = re.search(r"<PackageId>([^<]+)</PackageId>", text)
        ids.append(match.group(1) if match else os.path.splitext(os.path.basename(project))[0])
    return ids


def versions_on_nuget(package_id, source=FLAT_CONTAINER):
    """Every version NuGet has of a package; none when the package is not there yet."""
    url = f"{source}/{package_id.lower()}/index.json"
    try:
        with urllib.request.urlopen(url, timeout=30) as response:
            return json.load(response)["versions"]
    except urllib.error.HTTPError as error:
        if error.code == 404:
            return []
        raise


def highest_stable_patch(all_versions, base):
    """The highest number after the base among the stable versions (2026.10.40 is 40; 2026.10.41-rc.1 is not stable); -1 when there is none."""
    pattern = re.compile(rf"^{re.escape(base)}\.(\d+)$")
    patches = [int(match.group(1)) for version in all_versions if (match := pattern.match(version))]
    return max(patches) if patches else -1


def next_version(channel, base, patch, run=None, date=None):
    following = patch + 1
    if channel == "regular":
        return f"{base}.{following}"
    if run is None or not str(run).isdigit():
        raise ValueError("a candidate needs the number of the run (--run)")
    return f"{base}.{following}-rc.{date}.{run}"


def main():
    parser = argparse.ArgumentParser(description="The version a release publishes.")
    parser.add_argument("--channel", required=True, choices=["candidate", "regular"])
    parser.add_argument("--run")
    parser.add_argument("--date", default=datetime.datetime.now(datetime.timezone.utc).strftime("%y%m%d"))
    parser.add_argument("--base")
    parser.add_argument("--source", default=FLAT_CONTAINER)
    parser.add_argument("--offline", action="store_true")
    arguments = parser.parse_args()

    base = arguments.base or base_version_of_the_repository()
    if arguments.offline:
        patch = -1
    else:
        everything = []
        try:
            for package_id in package_ids():
                everything.extend(versions_on_nuget(package_id, arguments.source))
        except (urllib.error.URLError, TimeoutError) as error:
            # no guess: a number made without knowing what NuGet has could be one that exists
            print(f"::error::NuGet could not be asked for the versions that exist ({error}); use --offline only on a machine that is not releasing.", file=sys.stderr)
            return 3
        patch = highest_stable_patch(everything, base)

    try:
        version = next_version(arguments.channel, base, patch, arguments.run, arguments.date)
    except ValueError as error:
        print(f"::error::{error}", file=sys.stderr)
        return 2

    print(version)
    output = os.environ.get("GITHUB_OUTPUT")
    if output:
        with open(output, "a", encoding="utf-8") as handle:
            handle.write(f"version={version}\n")
            handle.write(f"previous_tag={'v' + base + '.' + str(patch) if patch >= 0 else ''}\n")
            handle.write(f"is_prerelease={'true' if arguments.channel == 'candidate' else 'false'}\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
