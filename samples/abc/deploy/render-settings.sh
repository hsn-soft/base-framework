#!/usr/bin/env bash
# Puts the passwords into the settings of an environment: for each host, appsettings.<environment>.json with CHANGE_ME replaced by the password from deploy/.env (a SQL Server connection string gets
# SQLSERVER_PASSWORD, a PostgreSQL one POSTGRES_PASSWORD), written to deploy/.rendered/<environment>/<host>.json (git ignores it). The committed files are the one place of the settings of an environment;
# the container gets the rendered file in place of the one in the image (compose: a bind mount; Kubernetes: a secret).
# Usage: samples/abc/deploy/render-settings.sh docker|uat|production
set -euo pipefail
cd "$(dirname "$0")/../../.."
environment="${1:?docker, uat or production}"
samples/abc/deploy/make-env.sh

python3 - "$environment" <<'PY'
import json, os, re, sys

environment = sys.argv[1]
secrets = dict(line.strip().split("=", 1) for line in open("samples/abc/deploy/.env") if "=" in line)
out_dir = f"samples/abc/deploy/.rendered/{environment}"
os.makedirs(out_dir, exist_ok=True)

def fill(value, where):
    if "CHANGE_ME" not in value:
        return value
    if value.startswith("Server="):
        return value.replace("CHANGE_ME", secrets["SQLSERVER_PASSWORD"])
    if value.startswith("Host="):
        return value.replace("CHANGE_ME", secrets["POSTGRES_PASSWORD"])
    sys.exit(f"{where}: CHANGE_ME in a value that has no known password")

def walk(node, where):
    if isinstance(node, dict):
        return {key: walk(value, f"{where}:{key}") for key, value in node.items()}
    if isinstance(node, list):
        return [walk(value, where) for value in node]
    return fill(node, where) if isinstance(node, str) else node

for line in open("samples/abc/deploy/hosts.txt"):
    if not line.strip() or line.startswith("#"):
        continue
    name, folder, _, _ = line.strip().split("|")
    source = f"samples/abc/{folder}/appsettings.{environment}.json"
    settings = json.loads(open(source).read())
    with open(f"{out_dir}/{name}.json", "w") as handle:
        json.dump(walk(settings, name), handle, indent=2)
    os.chmod(f"{out_dir}/{name}.json", 0o600)
print(f"Rendered the settings of {environment} to {out_dir}")
PY
