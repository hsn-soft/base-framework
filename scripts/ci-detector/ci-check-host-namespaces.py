#!/usr/bin/env python3
"""Checks the root namespace of the host projects (framework ADR-0158).

A host project (its name ends with .Http.Host, .Wcf.Host, .Grpc.Host or .Host) has <RootNamespace> set to its name without that suffix,
and no namespace in the code of the repository names the host suffix: not in `namespace`, `using`, `@namespace`, `@model` or `@using`
lines, and not in the `csharp_namespace` of a proto file. The namespace of a test project of a host (...Host.Tests) is its own name and is left alone.

Usage: scripts/ci-detector/ci-check-host-namespaces.py [folder]   (default: the current folder; exit code 1 when something breaks the rule)
"""
import os
import re
import sys

SKIP = {"bin", "obj", "node_modules", ".git", "TestResults", "tmp", ".idea", ".vs", ".vscode"}
SUFFIX = re.compile(r"\.(?:Http|Wcf|Grpc)\.Host$|\.Host$")
# a dotted name that has the segment Host, and the segment after it is not Tests
DOTTED = r"[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*"
LINE = re.compile(r"^\s*(?:global\s+)?(?:using\s+(?:static\s+)?(?:\w+\s*=\s*)?|namespace\s+|@using\s+|@namespace\s+|@model\s+|@inherits\s+)(" + DOTTED + r")")
PROTO = re.compile(r'option\s+csharp_namespace\s*=\s*"([^"]+)"')


def has_host_segment(name: str) -> bool:
    parts = name.split(".")
    for i, part in enumerate(parts):
        if part == "Host" and (i + 1 >= len(parts) or parts[i + 1] != "Tests"):
            return True
    return False


def walk(root):
    for folder, dirs, files in os.walk(root):
        dirs[:] = [d for d in dirs if d not in SKIP]
        for name in files:
            yield os.path.join(folder, name)


def main() -> int:
    root = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else ".")
    problems = []
    for path in walk(root):
        rel = os.path.relpath(path, root)
        if path.endswith(".csproj"):
            name = os.path.basename(path)[:-7]
            if not SUFFIX.search(name):
                continue
            expected = SUFFIX.sub("", name)
            text = open(path, encoding="utf-8-sig").read()
            found = re.search(r"<RootNamespace>([^<]*)</RootNamespace>", text)
            if not found or found.group(1) != expected:
                problems.append(f"{rel}: <RootNamespace> must be {expected} (the name of the host without its suffix), not {found.group(1) if found else 'unset'}")
        elif path.endswith((".cs", ".cshtml", ".razor")):
            try:
                lines = open(path, encoding="utf-8-sig").read().splitlines()
            except UnicodeDecodeError:
                continue
            for number, line in enumerate(lines, 1):
                match = LINE.match(line)
                if match and has_host_segment(match.group(1)):
                    problems.append(f"{rel}:{number}: the namespace {match.group(1)} names the host suffix")
        elif path.endswith(".proto"):
            for number, line in enumerate(open(path, encoding="utf-8-sig").read().splitlines(), 1):
                match = PROTO.search(line)
                if match and has_host_segment(match.group(1)):
                    problems.append(f"{rel}:{number}: the csharp_namespace {match.group(1)} names the host suffix")
    for problem in problems:
        print(f"::error::{problem}")
    if problems:
        print(f"{len(problems)} namespace problem(s): the root namespace of a host is its name without .Http.Host, .Wcf.Host, .Grpc.Host or .Host (ADR-0158)")
        return 1
    print("The root namespaces of the hosts follow the rule.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
