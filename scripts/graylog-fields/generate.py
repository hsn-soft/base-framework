#!/usr/bin/env python3
"""
Writes docs/GraylogFields.md: which fields each log type has, MEASURED from the logs of one run in Graylog (not declared anywhere in the code).

Run the samples against Graylog with a build number of your own (`BUILD_NUMBER=tbl123 FrameworkLogger__IsGrayLogActive=true ...`: the build number is
the `Version` field of every log), exercise every scenario, then:

    GRAYLOG_USER=... GRAYLOG_PASSWORD=... scripts/graylog-fields/generate.py tbl123 > docs/GraylogFields.md

GRAYLOG_URL defaults to http://localhost:9000. The credentials only come from the environment: they are never written anywhere.
"""
import base64
import collections
import json
import os
import sys
import urllib.parse
import urllib.request

TYPES = ["HttpRequest", "HttpResponse", "HttpRequestResponse", "ConsumeStart", "ConsumeEnd", "FrameworkLog", "PlatformLog", "SharedLog", "AppLog"]
SKIP = lambda key: key.startswith("gl2") or key in ("streams", "_id", "source", "timestamp", "version", "host", "full_message", "message", "level", "stringLevel")


def fetch(run: str) -> list:
    base = os.environ.get("GRAYLOG_URL", "http://localhost:9000")
    token = base64.b64encode(f"{os.environ['GRAYLOG_USER']}:{os.environ['GRAYLOG_PASSWORD']}".encode()).decode()
    messages = []
    for offset in range(0, 5000, 150):
        query = urllib.parse.urlencode({"query": f"Version:{run}", "range": "86400", "limit": "150", "offset": str(offset), "sort": "timestamp:asc"})
        request = urllib.request.Request(f"{base}/api/search/universal/relative?{query}", headers={"Accept": "application/json", "Authorization": f"Basic {token}"})
        with urllib.request.urlopen(request) as response:
            page = [m["message"] for m in json.load(response)["messages"]]
        messages += page
        if len(page) < 150:
            break
    return messages


def main() -> None:
    run = sys.argv[1]
    by_type = collections.defaultdict(list)
    for message in fetch(run):
        by_type[message.get("LogType", "(none)")].append(message)

    fields = sorted({key for logs in by_type.values() for log in logs for key in log if not SKIP(key)})

    def cell(log_type: str, field: str) -> str:
        logs = by_type[log_type]
        if not logs:
            return "?"
        present = sum(1 for log in logs if log.get(field) not in (None, ""))
        return "●" if present == len(logs) else ("◐" if present else "")

    owners = {field: [t for t in TYPES if cell(t, field) not in ("", "?")] for field in fields}
    only = lambda log_type: [f"`{field}`" for field in fields if owners[field] == [log_type]]

    print("# The fields of a log in Graylog\n")
    print(f"What each type of log has, **measured** from the logs of one real run of the samples (`Version:{run}`: a good order, a 400, a 500, a message that")
    print("failed, a request the gateway answered itself, a file upload and download, the log examples of the Abc identity service). Nothing in the code declares it:")
    print("the writer of a log puts its fields on it, the enrichers add the common ones and `NoiseEnricher` removes what a type must not have (see")
    print("[Logging.md](Logging.md), *Where each decision is made*). Regenerate it with `scripts/graylog-fields/generate.py` after a change to the fields.")
    print("`●` = every log of that type has it, `◐` = some do (it depends on the case: the user is known, there was an exception), empty = never.\n")
    print("Logs of this run: " + ", ".join(f"`{t}` {len(by_type[t])}" for t in TYPES) + "\n")
    print("| Alan | " + " | ".join(f"`{t}`" for t in TYPES) + " |")
    print("|---|" + ":-:|" * len(TYPES))
    for field in fields:
        print(f"| `{field}` | " + " | ".join(cell(t, field) for t in TYPES) + " |")
    print()
    for log_type in ("PlatformLog", "FrameworkLog"):
        print(f"**Only `{log_type}` has:** " + (", ".join(only(log_type)) or "nothing") + ".\n")
    print("`FrameworkLog` is not one shape: its `Facility` says which (`CONSUME_EVENT_RETRY`, `RABBITMQ_CONSUMER_*`, `RABBITMQ_CONNECTION_*`, ...) and the fields follow it.")
    print("`AppLog` is what its developer wrote (the arguments of the message are fields, `{@Object}` is one JSON field) plus the common fields and the `Facility` they")
    print("gave it with `BeginFacility`.")


if __name__ == "__main__":
    main()
