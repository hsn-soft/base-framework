#!/usr/bin/env bash
# Makes the secrets the compose file and the Kubernetes scripts use: random passwords in deploy/.env (git ignores it) and the keys of the tokens when they are not there yet. It never overwrites.
# The passwords are not in any file that is committed: the appsettings.docker.json files of the hosts have CHANGE_ME, and the compose file and the manifests take the real value from here.
set -euo pipefail
cd "$(dirname "$0")/../../.."

env_file=samples/abc/deploy/.env
if [ ! -f "$env_file" ]; then
    random() { openssl rand -hex 12; }
    {
        echo "POSTGRES_PASSWORD=$(random)"
        # SQL Server wants upper and lower case letters, a digit and a symbol
        echo "SQLSERVER_PASSWORD=Aa1!$(random)"
        echo "MONGO_PASSWORD=$(random)"
        echo "RABBITMQ_PASSWORD=$(random)"
    } > "$env_file"
    chmod 600 "$env_file"
    echo "Made $env_file"
fi

private=samples/abc/app/Abc.AuthServer.Host/private_key.xml
public=samples/abc/shared/Abc.Shared.Hosting/public_key.xml
if [ ! -f "$private" ] || [ ! -f "$public" ]; then
    rm -f "$private" "$public"
    dotnet run samples/scripts/generate-rsa-keys.cs -- "$private" "$public"
fi
