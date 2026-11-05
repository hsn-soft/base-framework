#!/usr/bin/env bash
# Makes the secrets of the deployments and the keys of the tokens, when they are not there yet (it never overwrites):
#  - deploy/.env (git ignores it) with the password of the local PostgreSQL, which the settings of the docker, uat and production environments leave as CHANGE_ME. It is the one of the development
#    environment of the same hosts (the infrastructure on this machine is the same for all of them), read from appsettings.development.json, so there is nothing to type;
#  - the key pair of the tokens (samples/scripts/generate-rsa-keys.cs).
set -euo pipefail
cd "$(dirname "$0")/../../.."

env_file=samples/hsx/deploy/.env
if [ ! -f "$env_file" ]; then
    python3 - "$env_file" <<'PY'
import json, re, sys

settings = json.loads(open("samples/hsx/service/content/Hsx.ContentService.Http.Host/appsettings.development.json").read())
for value in settings.get("ConnectionStrings", {}).values():
    if value.startswith("Host="):
        with open(sys.argv[1], "w") as out:
            out.write("POSTGRES_PASSWORD=" + re.search(r"Password=([^;]+)", value).group(1) + "\n")
        break
else:
    sys.exit("the development settings of the content service have no PostgreSQL connection string")
PY
    chmod 600 "$env_file"
    echo "Made $env_file"
fi

private=samples/hsx/app/Hsx.AuthServer.Host/private_key.xml
public=samples/hsx/shared/Hsx.Shared.Hosting.Http/public_key.xml
if [ ! -f "$private" ] || [ ! -f "$public" ]; then
    rm -f "$private" "$public"
    dotnet run samples/scripts/generate-rsa-keys.cs -- "$private" "$public"
fi
