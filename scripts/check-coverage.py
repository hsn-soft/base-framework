#!/usr/bin/env python3
"""Fails (exit 1) when line coverage of the given cobertura file(s) is below the threshold.

Usage: check-coverage.py <threshold-percent> <coverage.cobertura.xml> [...]
"""
import sys
import xml.etree.ElementTree as ET


def main() -> int:
    threshold = float(sys.argv[1])
    lines = {}
    for path in sys.argv[2:]:
        for cls in ET.parse(path).getroot().iter("class"):
            for line in cls.iter("line"):
                key = (cls.get("filename"), line.get("number"))
                lines[key] = max(lines.get(key, 0), int(line.get("hits")))

    total = len(lines)
    covered = sum(1 for hits in lines.values() if hits > 0)
    percent = 100.0 * covered / total if total else 0.0
    print(f"Line coverage: {covered}/{total} = {percent:.2f}% (threshold {threshold:.2f}%)")
    if percent + 1e-9 < threshold:
        print("::error::Line coverage is below the required threshold.")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
