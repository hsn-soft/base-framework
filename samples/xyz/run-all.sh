#!/usr/bin/env bash
# Builds and starts the commercial gateway, the auth server and the three services (environment "development": PostgreSQL, Redis and Kafka on localhost); Ctrl+C stops them all.
#   ./samples/xyz/run-all.sh
# The RSA key pair of the tokens must exist first (see samples/README.md, scripts/generate-rsa-keys.cs).
# Settings come from each appsettings.development.json; override them with environment variables, e.g.
#   ConnectionStrings__Redis=localhost:6380 ./samples/xyz/run-all.sh
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/../.."

projects=(Xyz.AuthServer.Host Xyz.ProductService.Host Xyz.ShipmentService.Host Xyz.OrderService.Host Xyz.Gateway.Commercial.Host)

for project in "${projects[@]}"; do
    dotnet build "samples/xyz/$project" --verbosity quiet
done

trap 'echo "Stopping..."; kill 0' INT TERM

for project in "${projects[@]}"; do
    name="${project#Xyz.}"
    ASPNETCORE_ENVIRONMENT=development dotnet run --no-build --project "samples/xyz/$project" 2>&1 \
        | awk -v name="${name%.Host}" '{ printf "[%s] %s\n", name, $0; fflush() }' &
done

echo "Gateway: http://localhost:5100   (try ./samples/xyz/smoke-test.sh once the services are up)"
wait
