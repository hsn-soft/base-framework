#!/usr/bin/env python3
"""Checks the references between the projects of src/ (ADR-0075).

  1. every package reaches HsnSoft.Base.Core, directly or through another framework package;
  2. a package never lists a reference that another one it lists already brings (Core included): if it references a sibling, it does not add Core,
     and it does not list a sibling that one of its siblings brings.

Usage: scripts/ci-check-framework-references.py   (exit code 1 and the offending projects when a rule is broken)
"""
import glob
import os
import re
import sys

CORE = "HsnSoft.Base.Core"
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")


def project_references():
    refs = {}
    for path in glob.glob(os.path.join(ROOT, "src", "*", "*.csproj")):
        name = os.path.basename(path).removesuffix(".csproj")
        text = open(path, encoding="utf-8-sig").read()
        refs[name] = [re.split(r"[\\/]", ref)[-1].removesuffix(".csproj") for ref in re.findall(r'ProjectReference Include="([^"]+)"', text)]
    return refs


def brought_by(refs, name):
    seen, pending = set(), list(refs.get(name, []))
    while pending:
        item = pending.pop()
        if item not in seen:
            seen.add(item)
            pending += refs.get(item, [])
    return seen


def main() -> int:
    refs = project_references()
    problems = []
    for name in sorted(refs):
        if name != CORE and CORE not in brought_by(refs, name):
            problems.append(f"{name}: does not reach {CORE}; reference it (or a framework package that does)")
        for item in refs[name]:
            brought_by_sibling = [other for other in refs[name] if other != item and item in brought_by(refs, other)]
            if brought_by_sibling:
                problems.append(f"{name}: lists {item}, which {brought_by_sibling[0]} already brings; remove the reference")
    if problems:
        print("The references between the framework projects break ADR-0075:")
        print("\n".join(f"  {problem}" for problem in problems))
        return 1
    print(f"{len(refs)} framework projects: every one reaches Core and none lists a reference a sibling brings.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
