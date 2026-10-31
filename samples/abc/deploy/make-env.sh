#!/usr/bin/env bash
# Makes the secrets of the deployments and the keys of the tokens, when they are not there yet (it never overwrites):
#  - deploy/.env (git ignores it) with the passwords of the local PostgreSQL and SQL Server, which the settings of the docker, uat and production environments leave as CHANGE_ME. They are the ones of the development
#    environment of the same hosts (the infrastructure on this machine is the same for all of them), read from appsettings.development.json, so there is nothing to type;
#  - the key pair of the tokens (samples/scripts/generate-rsa-keys.cs).
set -euo pipefail
cd "$(dirname "$0")/../../.."

env_file=samples/abc/deploy/.env
if [ ! -f "$env_file" ]; then
    python3 - "$env_file" <<'PY'
import json, re, sys

def password(file, prefix):
    settings = json.loads(re.sub(r'^\s*//.*$', '', open(file).read(), flags=re.M))
    for value in settings.get("ConnectionStrings", {}).values():
        if value.startswith(prefix):
            return re.search(r"Password=([^;]+)", value).group(1)
    sys.exit(f"{file} has no connection string that starts with {prefix}")

with open(sys.argv[1], "w") as out:
    out.write("POSTGRES_PASSWORD=" + password("samples/abc/service/shipment/Abc.ShipmentService.Http.Host/appsettings.development.json", "Host=") + "\n")
    out.write("SQLSERVER_PASSWORD=" + password("samples/abc/service/identity/Abc.IdentityService.Http.Host/appsettings.development.json", "Server=") + "\n")
PY
    chmod 600 "$env_file"
    echo "Made $env_file"
fi

private=samples/abc/app/Abc.AuthServer.Host/private_key.xml
public=samples/abc/shared/Abc.Shared.Hosting/public_key.xml
if [ ! -f "$private" ] || [ ! -f "$public" ]; then
    rm -f "$private" "$public"
    dotnet run samples/scripts/generate-rsa-keys.cs -- "$private" "$public"
fi
