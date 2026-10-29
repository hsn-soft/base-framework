#!/usr/bin/env python3
"""Fails (exit 1) when line coverage of the given cobertura file(s) is below the threshold.

Several reports (one per test project) are merged: a line counts as covered when any report covered it.

Usage: ci-check-coverage.py <threshold-percent> <coverage.cobertura.xml> [...]
"""
import os
import sys
import xml.etree.ElementTree as ET


def absolute_path(sources, filename):
    """coverlet writes file names relative to a source root that differs per report (the common root of the projects in it)."""
    for source in sources:
        candidate = os.path.normpath(os.path.join(source, filename))
        if os.path.exists(candidate):
            return candidate
    return os.path.normpath(os.path.join(sources[0], filename)) if sources else filename


def main() -> int:
    threshold = float(sys.argv[1])
    lines = {}
    for path in sys.argv[2:]:
        root = ET.parse(path).getroot()
        sources = [source.text for source in root.iter("source") if source.text]
        for cls in root.iter("class"):
            filename = absolute_path(sources, cls.get("filename"))
            for line in cls.iter("line"):
                key = (filename, line.get("number"))
                lines[key] = max(lines.get(key, 0), int(line.get("hits")))

    total = len(lines)
    covered = sum(1 for hits in lines.values() if hits > 0)
    percent = 100.0 * covered / total if total else 0.0
    print(f"Line coverage: {covered}/{total} = {percent:.2f}% (threshold {threshold:.2f}%)")
    if percent + 1e-9 < threshold:
        for (filename, number), hits in sorted(lines.items()):
            if hits == 0:
                print(f"  not covered: {filename}:{number}")
        print("::error::Line coverage is below the required threshold.")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
