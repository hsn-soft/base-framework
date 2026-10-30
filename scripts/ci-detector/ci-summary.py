#!/usr/bin/env python3
"""Prints the summary of a CI run: how long each step took, how many tests passed, and the line coverage.

Usage: ci-summary.py <timings dir> <test results dir> [--badges <dir> [--result <success|failure>]]

  <timings dir>       the files written by ci-timed.sh, one per job (unit.txt, integration.txt): "label=seconds" lines
  <test results dir>  the downloaded test results of the jobs: .trx files (the counts) and coverage.cobertura.xml files (the coverage)

  --badges <dir>      also writes the endpoint files of the README badges there (shields.io "endpoint" JSON): coverage.json, unit-tests.json,
                      integration-tests.json and checked.json (the time of the check as yyyyMMddHHmm, UTC); the colors say whether the numbers are good
  --result <status>   how the pipeline ended; "failure" makes the "checked" badge red (the default is "success")

The summary goes to the log and, on GitHub Actions, to the page of the run (GITHUB_STEP_SUMMARY). It never fails the run: it only reports.
"""
import argparse
import datetime
import glob
import importlib.util
import json
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
    # in the order of the pipeline
    order = ["rules-build", "unit", "integration"]
    return dict(sorted(jobs.items(), key=lambda item: order.index(item[0]) if item[0] in order else len(order)))


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


def write_badges(directory, results, result):
    """The endpoint files of the badges of the README: shields.io reads them from the branch they are published to."""
    threshold = float(os.environ.get("COVERAGE_THRESHOLD", "99"))
    os.makedirs(directory, exist_ok=True)

    def badge(name, label, message, color):
        with open(os.path.join(directory, name + ".json"), "w", encoding="utf-8") as handle:
            json.dump({"schemaVersion": 1, "label": label, "message": message, "color": color}, handle)

    coverage = read_coverage(results)
    if coverage and coverage[1]:
        percent = 100.0 * coverage[0] / coverage[1]
        badge("coverage", "coverage", f"{percent:.2f}%", "brightgreen" if percent + 1e-9 >= threshold else "red")
    else:
        badge("coverage", "coverage", "unknown", "lightgrey")

    for kind, label in (("unit", "unit tests"), ("integration", "integration tests")):
        passed, failed, total = read_counts(results, kind)
        if total == 0:
            badge(f"{kind}-tests", label, "unknown", "lightgrey")
        elif failed:
            badge(f"{kind}-tests", label, f"{passed} passed, {failed} failed", "red")
        else:
            badge(f"{kind}-tests", label, f"{passed} passed", "brightgreen")

    # when the numbers above were checked, to the minute (UTC): the badges are not made by every merge
    checked = datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%d%H%M")
    badge("checked", "checked (UTC)", checked, "blue" if result == "success" else "red")


def main():
    parser = argparse.ArgumentParser(description="The summary of a CI run.")
    parser.add_argument("timings")
    parser.add_argument("results")
    parser.add_argument("--badges")
    parser.add_argument("--result", default="success", choices=["success", "failure"])
    arguments = parser.parse_args()
    timings = read_timings(arguments.timings)
    results = arguments.results
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
    if job_totals:
        out.append(
            "The jobs run one after the other: " + ", ".join(f"**{job}** {minutes(total)}" for job, total in job_totals.items())
            + f"; the steps add up to **{minutes(sum(job_totals.values()))}** (the minutes of the runners are the sum of the jobs, each rounded up to a whole minute), plus the time each job takes to start and to pass the build on."
        )

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
    if arguments.badges:
        write_badges(arguments.badges, results, arguments.result)
    return 0


if __name__ == "__main__":
    sys.exit(main())
