#!/usr/bin/env bash
# Makes the secrets of the deployments and the keys of the tokens, when they are not there yet (it never overwrites):
#  - deploy/.env (git ignores it) with the password of the local SQL Server, which the settings of the docker, uat and production environments leave as CHANGE_ME (it is the one of the development settings of
#    the identity service, the infrastructure on this machine is the same for all environments), and the user of the local MinIO, which the hosts need (Minio__AccessKey and Minio__SecretKey): from
#    samples/edx/.env or the environment when they are there, otherwise from the container of the local MinIO (infra-minio_container) that is running;
#  - the key pair of the tokens (samples/scripts/generate-rsa-keys.cs).
set -euo pipefail
cd "$(dirname "$0")/../../.."

env_file=samples/edx/deploy/.env
if [ ! -f "$env_file" ]; then
    access="${Minio__AccessKey:-}"; secret="${Minio__SecretKey:-}"
    if [ -z "$access" ] && [ -f samples/edx/.env ]; then
        access="$(sed -n 's/^Minio__AccessKey=//p' samples/edx/.env)"; secret="$(sed -n 's/^Minio__SecretKey=//p' samples/edx/.env)"
    fi
    if [ -z "$access" ]; then
        access="$(docker inspect infra-minio_container --format '{{range .Config.Env}}{{println .}}{{end}}' 2>/dev/null | sed -n 's/^MINIO_ROOT_USER=//p')"
        secret="$(docker inspect infra-minio_container --format '{{range .Config.Env}}{{println .}}{{end}}' 2>/dev/null | sed -n 's/^MINIO_ROOT_PASSWORD=//p')"
    fi
    [ -n "$access" ] && [ -n "$secret" ] || { echo "No MinIO user: set Minio__AccessKey and Minio__SecretKey, put them in samples/edx/.env, or run infra-minio_container" >&2; exit 1; }
    python3 - "$env_file" "$access" "$secret" <<'PY'
import json, re, sys

settings = json.loads(re.sub(r'^\s*//.*$', '', open("samples/edx/service/identity/Edx.IdentityService.Http.Host/appsettings.development.json").read(), flags=re.M))
sql = next(v for v in settings["ConnectionStrings"].values() if v.startswith("Server="))
with open(sys.argv[1], "w") as out:
    out.write("SQLSERVER_PASSWORD=" + re.search(r"Password=([^;]+)", sql).group(1) + "\n")
    out.write("Minio__AccessKey=" + sys.argv[2] + "\n")
    out.write("Minio__SecretKey=" + sys.argv[3] + "\n")
PY
    chmod 600 "$env_file"
    echo "Made $env_file"
fi

private=samples/edx/app/Edx.AuthServer.Host/private_key.xml
public=samples/edx/shared/Edx.Shared.Hosting/public_key.xml
if [ ! -f "$private" ] || [ ! -f "$public" ]; then
    rm -f "$private" "$public"
    dotnet run samples/scripts/generate-rsa-keys.cs -- "$private" "$public"
fi
