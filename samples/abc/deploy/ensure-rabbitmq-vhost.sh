#!/usr/bin/env bash
# Makes the virtual host of an environment on the RabbitMQ of this machine (through its management API, port 15672), with the user the hosts connect with allowed on it. A virtual host is the isolation
# RabbitMQ has: the exchanges and queues of uat and production never meet those of development, whatever they are called. It does nothing when the virtual host is there.
# Usage: samples/abc/deploy/ensure-rabbitmq-vhost.sh uat|prod
set -euo pipefail
cd "$(dirname "$0")/../../.."
vhost="${1:?the name of the virtual host}"

read -r user password < <(python3 - <<'PY'
import json, re
settings = json.loads(re.sub(r'^\s*//.*$', '', open("samples/abc/service/shipment/Abc.ShipmentService.Http.Host/appsettings.development.json").read(), flags=re.M))
connection = settings["RabbitMq"]["Connection"]
print(connection["UserName"], connection["Password"])
PY
)
api="http://localhost:15672/api"
curl -fsS -u "$user:$password" -X PUT "$api/vhosts/$vhost" >/dev/null
curl -fsS -u "$user:$password" -X PUT -H 'content-type: application/json' -d '{"configure":".*","write":".*","read":".*"}' "$api/permissions/$vhost/$user" >/dev/null
echo "RabbitMQ virtual host $vhost is there"
