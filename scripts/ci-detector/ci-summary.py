#!/usr/bin/env python3
"""Prints the summary of a CI run: how long each step took, how many tests passed, and the line coverage.

Usage: ci-summary.py <timings dir> <test results dir>

  <timings dir>       the files written by ci-timed.sh, one per job (unit.txt, integration.txt): "label=seconds" lines
  <test results dir>  the downloaded test results of the jobs: .trx files (the counts) and coverage.cobertura.xml files (the coverage)

The summary goes to the log and, on GitHub Actions, to the page of the run (GITHUB_STEP_SUMMARY). It never fails the run: it only reports.
"""
import glob
import importlib.util
import os
import sys
import xml.etree.ElementTree as ET

TRX = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


def minutes(seconds):
    return f"{int(seconds) // 60}m {int(seconds) % 60:02d}s"


def read_timings(directory):
    """{job: [(label, seconds)]} from the files of the jobs."""
    jobs = {}
    for path in sorted(glob.glob(os.path.join(directory, "**", "*.txt"), recursive=True)):
        steps = []
        for line in open(path, encoding="utf-8"):
            label, _, seconds = line.strip().rpartition("=")
            if label and seconds.isdigit():
                steps.append((label, int(seconds)))
        if steps:
            jobs[os.path.splitext(os.path.basename(path))[0]] = steps
    return jobs


def read_counts(directory, kind):
    """(passed, failed, total) over the .trx files of one kind of test (the folder of the job is named after it)."""
    passed = failed = total = 0
    for path in glob.glob(os.path.join(directory, "**", "*.trx"), recursive=True):
        if f"{os.sep}{kind}-test-results{os.sep}" not in path and f"{os.sep}{kind}{os.sep}" not in path:
            continue
        counters = ET.parse(path).getroot().find(f".//{TRX}Counters")
        if counters is not None:
            passed += int(counters.get("passed", 0))
            failed += int(counters.get("failed", 0))
            total += int(counters.get("total", 0))
    return passed, failed, total


def read_coverage(directory):
    """(covered, total) lines of all the coverage reports, merged the way the gate merges them."""
    reports = glob.glob(os.path.join(directory, "**", "coverage.cobertura.xml"), recursive=True)
    if not reports:
        return None
    spec = importlib.util.spec_from_file_location("coverage_gate", os.path.join(os.path.dirname(__file__), "ci-check-coverage.py"))
    gate = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(gate)
    lines = gate.merge_lines(reports)
    return sum(1 for hits in lines.values() if hits > 0), len(lines)


def main():
    timings = read_timings(sys.argv[1])
    results = sys.argv[2]
    out = []

    out.append("## Time of every step")
    out.append("")
    out.append("| Job | Step | Time |")
    out.append("|---|---|---|")
    job_totals = {}
    for job, steps in timings.items():
        for label, seconds in steps:
            out.append(f"| {job} | {label} | {minutes(seconds)} |")
        job_totals[job] = sum(seconds for _, seconds in steps)
    out.append("")
    out.append("The jobs run at the same time: " + ", ".join(f"**{job}** {minutes(total)}" for job, total in job_totals.items()) + (f"; the run takes about **{minutes(max(job_totals.values()))}** plus the gate." if job_totals else "."))

    out.append("")
    out.append("## Tests and coverage")
    out.append("")
    out.append("| What | Result |")
    out.append("|---|---|")
    for kind in ("unit", "integration"):
        passed, failed, total = read_counts(results, kind)
        out.append(f"| {kind} tests | {passed}/{total} passed" + (f", **{failed} failed**" if failed else "") + " |")
    coverage = read_coverage(results)
    if coverage:
        covered, total = coverage
        out.append(f"| line coverage of src (unit and integration merged) | {100.0 * covered / total:.2f}% ({covered}/{total} lines) |")
    else:
        out.append("| line coverage | no report found |")

    text = "\n".join(out)
    print(text)
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write(text + "\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
