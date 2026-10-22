#!/usr/bin/env bash
# Builds and starts the gateway, the auth server and the three services; Ctrl+C stops them all.
#   ./samples/run-all.sh
# Connection settings come from each appsettings.json; override them with environment variables, e.g.
#   ConnectionStrings__Redis=localhost:6380 ./samples/run-all.sh
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

projects=(Xyz.AuthServer.Host Xyz.ServiceProduct.Host Xyz.ServiceShipment.Host Xyz.ServiceOrder.Host Xyz.Gateway.Host)

for project in "${projects[@]}"; do
    dotnet build "samples/$project" --configuration Release --verbosity quiet
done

trap 'echo "Stopping..."; kill 0' INT TERM

for project in "${projects[@]}"; do
    name="${project#Xyz.}"
    dotnet run --no-build --configuration Release --project "samples/$project" 2>&1 \
        | awk -v name="${name%.Host}" '{ printf "[%s] %s\n", name, $0; fflush() }' &
done

echo "Gateway: http://localhost:5100   (try ./samples/smoke-test.sh once the services are up)"
wait
