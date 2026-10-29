#!/usr/bin/env bash
# Builds and starts the Abc hosts of the order flow (identity, auth server, product, shipment, order, commercial gateway) in the development environment. Ctrl+C stops them.
# Graylog is off unless PersistentLogger__IsGrayLogActive=true is in the environment (then the logs go to the local infra-graylog).
set -euo pipefail
cd "$(dirname "$0")/../.."

hosts=(
  samples/abc/service/identity/Abc.IdentityService.Http.Host
  samples/abc/app/Abc.AuthServer.Host
  samples/abc/service/product/Abc.ProductService.Http.Host
  samples/abc/service/shipment/Abc.ShipmentService.Http.Host
  samples/abc/service/order/Abc.OrderService.Http.Host
  samples/abc/gateway/Abc.Gateway.Commercial.Host
)

for host in "${hosts[@]}"; do
  dotnet build "$host" --verbosity quiet 2>&1 | grep -E "error|rror\(s\)" | head -3 || true
done

export ASPNETCORE_ENVIRONMENT=development
trap 'kill 0' INT TERM
for host in "${hosts[@]}"; do
  dotnet run --no-build --project "$host" 2>&1 | awk -v name="$(basename "$host")" '{ printf "[%s] %s\n", name, $0; fflush() }' &
done
wait
