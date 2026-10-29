#!/usr/bin/env python3
"""Checks (and with --fix, rewrites) the order of the references in every project file (ADR-0043).

The references of a project go in groups, one <ItemGroup> for each kind, top to bottom:

  1. external   FrameworkReference and PackageReference of NuGet packages that are not ours (Microsoft.*, Yarp.ReverseProxy, Newtonsoft.Json)
  2. framework  HsnSoft.Base.*          (a ProjectReference to src/ or a PackageReference)
  3. shared     <Product>.Shared.*      ({Product}.Shared.Hosting, Hhs.Shared.Helper)
  4. own        everything else         (the other projects of the same service or product)

Inside a group the items are in alphabetical order of their names. Groups that hold something other than references
(Content, EmbeddedResource, None...) come before the reference groups. A project with nothing of a kind has no group of it.

Usage: scripts/ci-force-csproj-order.py [--fix] [project files...]   (without files: every *.csproj of the repository)
"""
import os
import re
import subprocess
import sys

REFERENCE = re.compile(r'<(FrameworkReference|PackageReference|ProjectReference)\b')
KINDS = ["external", "framework", "shared", "own"]


def tag_of(item: str) -> str:
    # the comments that stand above an item are part of it, so the element is looked for, not assumed to be first
    return REFERENCE.search(item).group(1)


def name_of(item: str) -> str:
    include = re.search(r'Include="([^"]+)"', item).group(1)
    if tag_of(item) == "ProjectReference":
        return re.split(r"[\\/]", include)[-1].removesuffix(".csproj")
    return include


def kind_of(item: str) -> str:
    name = name_of(item)
    if name.startswith("HsnSoft.Base."):
        return "framework"
    if re.match(r"^[A-Za-z0-9]+\.Shared\.", name):
        return "shared"
    if tag_of(item) == "ProjectReference":
        return "own"
    return "external"


def sort_key(item: str):
    # FrameworkReference first, then packages, by name
    return (0 if tag_of(item) == "FrameworkReference" else 1, name_of(item).lower())


def split_items(body: str):
    """The items of an ItemGroup body, each with the comment lines that stand above it. Returns (items, other_text_or_None)."""
    items, pending, current = [], [], None
    for line in body.strip("\n").split("\n"):
        stripped = line.strip()
        if current is not None:
            current.append(line)
            if stripped.endswith("</PackageReference>") or stripped.endswith("</ProjectReference>") or stripped.endswith("</FrameworkReference>"):
                items.append("\n".join(pending + current))
                pending, current = [], None
            continue
        if not stripped:
            continue
        if stripped.startswith("<!--"):
            pending.append(line)
        elif REFERENCE.match(stripped):
            if stripped.endswith("/>"):
                items.append("\n".join(pending + [line]))
                pending = []
            else:
                current = [line]
        else:
            return None
    return items


def groups_of(text: str):
    return [(m.start(), m.end(), m.group(1)) for m in re.finditer(r"[ \t]*<ItemGroup>(.*?)</ItemGroup>[ \t]*\n?", text, re.S)]


def check(path: str, fix: bool) -> list[str]:
    text = open(path, encoding="utf-8").read()
    problems, reference_groups, other_after_reference = [], [], False
    for start, end, body in groups_of(text):
        items = split_items(body)
        if items is None or not items:
            if reference_groups:
                other_after_reference = True
            continue
        kinds = {kind_of(item) for item in items}
        reference_groups.append((start, end, items, kinds))
    if other_after_reference:
        problems.append("a group of files (Content, EmbeddedResource...) is after a group of references")
    order = [next(iter(kinds)) if len(kinds) == 1 else "mixed" for _, _, _, kinds in reference_groups]
    if "mixed" in order:
        problems.append("a group mixes kinds of references (" + ", ".join(KINDS) + " each have their own ItemGroup)")
    known = [kind for kind in order if kind != "mixed"]
    if len(set(known)) != len(known):
        problems.append("a kind of reference has more than one ItemGroup")
    if known != sorted(known, key=KINDS.index):
        problems.append("the groups are not external, framework, shared, own: found " + ", ".join(order))
    for (_, _, items, _), kind in zip(reference_groups, order):
        names = [sort_key(item) for item in items]
        if names != sorted(names):
            problems.append(f"the {kind} group is not in alphabetical order")
    if problems and fix:
        every = [item for _, _, items, _ in reference_groups for item in items]
        grouped = {kind: sorted([item for item in every if kind_of(item) == kind], key=sort_key) for kind in KINDS}
        blocks = ["    <ItemGroup>\n" + "\n".join(grouped[kind]) + "\n    </ItemGroup>\n" for kind in KINDS if grouped[kind]]
        if reference_groups:
            # the reference groups come out, and the new ones go after the last group of files (or where the first reference group was, when the project has none)
            pieces, cursor = [], 0
            for start, end, _, _ in reference_groups:
                pieces.append(text[cursor:start])
                cursor = end
            pieces.append(text[cursor:])
            remaining = "".join(pieces)
            insert_at = None
            for _, end, _ in groups_of(remaining):
                insert_at = end
            if insert_at is None:
                insert_at = len(pieces[0])
            new_text = remaining[:insert_at].rstrip("\n") + "\n\n" + "\n".join(blocks) + "\n" + remaining[insert_at:].lstrip("\n")
            new_text = re.sub(r"\n{3,}", "\n\n", new_text)
            if not new_text.endswith("\n"):
                new_text += "\n"
            open(path, "w", encoding="utf-8").write(new_text)
        return []
    return problems


def main() -> int:
    args = sys.argv[1:]
    fix = "--fix" in args
    files = [a for a in args if not a.startswith("--")]
    if not files:
        root = subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True, check=True).stdout.strip()
        files = []
        for folder, dirs, names in os.walk(root):
            dirs[:] = [d for d in dirs if d not in ("obj", "bin", ".git", "node_modules", "TestResults")]
            files += [os.path.join(folder, n) for n in names if n.endswith(".csproj")]
    failed = 0
    for path in sorted(files):
        for problem in check(path, fix):
            print(f"::error file={os.path.relpath(path)}::{problem}")
            failed = 1
    if not failed:
        print("The references of every project file are in order." if not fix else "Project files fixed.")
    else:
        print("Run scripts/ci-force-csproj-order.py --fix to put them in order.")
    return failed


if __name__ == "__main__":
    sys.exit(main())
