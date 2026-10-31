#!/usr/bin/env bash
# Puts the secrets into the settings of an environment: for each host, appsettings.<environment>.json with CHANGE_ME replaced by the password of SQL Server from deploy/.env, and the user of MinIO (AccessKey and
# SecretKey) added to the hosts that have a Minio section, written to deploy/.rendered/<environment>/<host>.json (git ignores it). The committed files are the one place of the settings of an environment;
# the container gets the rendered file in place of the one in the image (compose: a bind mount; Kubernetes: a secret).
# Usage: samples/edx/deploy/render-settings.sh docker|uat|production
set -euo pipefail
cd "$(dirname "$0")/../../.."
environment="${1:?docker, uat or production}"
samples/edx/deploy/make-env.sh

python3 - "$environment" <<'PY'
import json, os, sys

environment = sys.argv[1]
secrets = dict(line.rstrip("\n").split("=", 1) for line in open("samples/edx/deploy/.env") if "=" in line)
out_dir = f"samples/edx/deploy/.rendered/{environment}"
os.makedirs(out_dir, exist_ok=True)

def fill(value, where):
    if "CHANGE_ME" not in value:
        return value
    if value.startswith("Server="):
        return value.replace("CHANGE_ME", secrets["SQLSERVER_PASSWORD"])
    sys.exit(f"{where}: CHANGE_ME in a value that has no known password")

def walk(node, where):
    if isinstance(node, dict):
        return {key: walk(value, f"{where}:{key}") for key, value in node.items()}
    if isinstance(node, list):
        return [walk(value, where) for value in node]
    return fill(node, where) if isinstance(node, str) else node

for line in open("samples/edx/deploy/hosts.txt"):
    if not line.strip() or line.startswith("#"):
        continue
    name, folder, _, kind, _, _ = line.strip().split("|")
    if kind == "spa":
        continue
    settings = json.loads(open(f"samples/edx/{folder}/appsettings.{environment}.json").read())
    settings = walk(settings, name)
    if "Minio" in settings:
        settings["Minio"]["AccessKey"] = secrets["Minio__AccessKey"]
        settings["Minio"]["SecretKey"] = secrets["Minio__SecretKey"]
    with open(f"{out_dir}/{name}.json", "w") as handle:
        json.dump(settings, handle, indent=2)
    os.chmod(f"{out_dir}/{name}.json", 0o600)
print(f"Rendered the settings of {environment} to {out_dir}")
PY
