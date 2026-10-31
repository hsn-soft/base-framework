#!/usr/bin/env python3
"""Formats (or verifies) the given C# files with the solution that owns them.

The repository can have more than one solution. A file belongs to the solution in the deepest folder above it, and `dotnet format` only knows the files of the
solution it is given, so each group of files is given to its own solution (a file of no solution is left alone).

Usage: scripts/ci-detector/format-staged.py <whitespace|style|verify-style> <file>...
  whitespace    dotnet format whitespace                              (rewrites the files)
  style         dotnet format style --severity info                   (rewrites the files)
  verify-style  dotnet format style --severity info --verify-no-changes   (a non-zero exit code when a file would change)

The style rules (IDE0005, an unnecessary using) need the references of the projects to be resolved: where the build output is not there (a clean, the CI detector, a fresh clone)
`dotnet format` cannot find the namespaces of the other projects and takes every using of them for unnecessary, and removes it, which breaks the build. So the solution is built first
(a restore and a build; a no-op where nothing changed) and, when it does not build, nothing is formatted.
"""
import os
import subprocess
import sys


def main() -> int:
    if len(sys.argv) < 3:
        return 0
    mode, files = sys.argv[1], sys.argv[2:]
    root = subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True, check=True).stdout.strip()
    os.chdir(root)
    solutions = subprocess.run(["git", "ls-files", "*.sln"], capture_output=True, text=True, check=True).stdout.split()
    solutions.sort(key=lambda path: path.count("/"), reverse=True)  # the deepest folder first

    groups: dict[str, list[str]] = {}
    for file in files:
        normalized = file.replace("\\", "/")
        for solution in solutions:
            folder = os.path.dirname(solution)
            if folder == "" or normalized.startswith(folder + "/"):
                groups.setdefault(solution, []).append(normalized)
                break

    commands = {
        "whitespace": ["dotnet", "format", "whitespace", "{sln}", "--include"],
        "style": ["dotnet", "format", "style", "{sln}", "--severity", "info", "--no-restore", "--include"],
        "verify-style": ["dotnet", "format", "style", "{sln}", "--severity", "info", "--no-restore", "--verify-no-changes", "--include"],
    }
    if mode not in commands:
        print(f"unknown mode '{mode}'", file=sys.stderr)
        return 1

    status = 0
    for solution, group in groups.items():
        if mode != "whitespace":
            built = subprocess.run(["dotnet", "build", solution, "--nologo", "--verbosity", "quiet", "-p:TreatWarningsAsErrors=false"], check=False, capture_output=True, text=True)
            if built.returncode != 0:
                print(f"{solution} does not build, so its style cannot be formatted (the usings would be taken for unnecessary); fix the build first:\n" + "\n".join(built.stdout.splitlines()[-15:]), file=sys.stderr)
                return built.returncode
        command = [part.replace("{sln}", solution) for part in commands[mode]] + group
        code = subprocess.run(command, check=False).returncode
        status = status or code
    return status


if __name__ == "__main__":
    sys.exit(main())
