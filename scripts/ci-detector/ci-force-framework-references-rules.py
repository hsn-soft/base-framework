#!/usr/bin/env python3
"""Checks the references between the projects of src/ (ADR-0075). Every project except HsnSoft.Base.Core:

  1. has at least one project reference, and reaches HsnSoft.Base.Core through them (directly or through another framework package);
  2. when it has more than one reference, none of them is Core: a sibling it references brings Core already;
  3. when it has exactly one reference, that one is Core, or a project that reaches Core;
  4. never lists a reference that another one it lists already brings (a sibling that a sibling brings).

Usage: scripts/ci-force-framework-references-rules.py [--src <directory of the framework projects>]
Exit code 1 and the offending projects (with the rule they break) when a rule is broken.
"""
import glob
import os
import re
import sys

CORE = "HsnSoft.Base.Core"


def project_references(src: str):
    refs = {}
    for path in glob.glob(os.path.join(src, "*", "*.csproj")):
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


def reaches_core(refs, name) -> bool:
    return name == CORE or CORE in brought_by(refs, name)


def check(refs):
    problems = []
    for name in sorted(refs):
        if name == CORE:
            continue
        listed = refs[name]
        if not listed:
            problems.append(f"{name}: rule 1: has no project reference; it must reference {CORE}, or a framework package that reaches it")
            continue
        if len(listed) > 1 and CORE in listed:
            others = ", ".join(item for item in listed if item != CORE)
            problems.append(f"{name}: rule 2: lists {CORE} beside {others}, which already bring it; remove the {CORE} reference")
        if len(listed) == 1 and not reaches_core(refs, listed[0]):
            problems.append(f"{name}: rule 3: its only reference, {listed[0]}, is not {CORE} and does not reach it")
        if not reaches_core(refs, name) and listed:
            problems.append(f"{name}: rule 1: does not reach {CORE} through {', '.join(listed)}")
        for item in listed:
            if item == CORE and len(listed) > 1:
                continue  # said by rule 2
            brought = [other for other in listed if other != item and item in brought_by(refs, other)]
            if brought:
                problems.append(f"{name}: rule 4: lists {item}, which {brought[0]} already brings; remove the reference")
    return problems


def main(argv) -> int:
    src = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "src")
    if "--src" in argv:
        src = argv[argv.index("--src") + 1]
    refs = project_references(src)
    problems = check(refs)
    if problems:
        print("The references between the framework projects break ADR-0075:")
        print("\n".join(f"  {problem}" for problem in problems))
        return 1
    print(f"{len(refs)} framework projects: every one but Core reaches Core, with no Core listed beside another reference and nothing listed that a sibling brings.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
