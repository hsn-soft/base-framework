#!/usr/bin/env bash
# Builds and starts the Edx hosts (identity service, auth server, the firm invoice integration (REST, gRPC and WCF hosts), invoice service, signer service, the GİB invoice integration (REST and WCF hosts), the mock GİB center, gateway) and the back office (Angular, http://localhost:4200; npm install is run once) in the development environment. Ctrl+C stops them.
# The local infrastructure on its default ports is used: SQL Server, MongoDB, Redis, Kafka and MinIO (localhost:9100). MinIO needs a user: put it in samples/edx/.env (git ignores *.env):
#   Minio__AccessKey=...
#   Minio__SecretKey=...
# or export the two variables. Graylog is off unless PersistentLogger__IsGrayLogActive=true is in the environment (then the logs go to the local infra-graylog).
set -euo pipefail
cd "$(dirname "$0")/../.."

if [ -f samples/edx/.env ]; then
  set -a
  # shellcheck disable=SC1091
  . samples/edx/.env
  set +a
fi
: "${Minio__AccessKey:?Minio__AccessKey is not set (see the header of this script)}"
: "${Minio__SecretKey:?Minio__SecretKey is not set (see the header of this script)}"

hosts=(
  samples/edx/service/identity/Edx.IdentityService.Http.Host
  samples/edx/app/Edx.AuthServer.Host
  samples/edx/integration/firm-invoice/Edx.Integration.FirmInvoice.Http.Host
  samples/edx/integration/firm-invoice/Edx.Integration.FirmInvoice.Grpc.Host
  samples/edx/integration/firm-invoice/Edx.Integration.FirmInvoice.Wcf.Host
  samples/edx/service/invoice/Edx.InvoiceService.Http.Host
  samples/edx/service/signer/Edx.SignerService.Http.Host
  samples/edx/integration/gib-invoice/Edx.Integration.GibInvoice.Http.Host
  samples/edx/integration/gib-invoice/Edx.Integration.GibInvoice.Wcf.Host
  samples/edx/mock/Edx.Mock.Center.Wcf.Host
  samples/edx/gateway/Edx.Gateway.Host
)

for host in "${hosts[@]}"; do
  dotnet build "$host" --verbosity quiet 2>&1 | grep -E "error|rror\(s\)" | head -3 || true
done

export ASPNETCORE_ENVIRONMENT=development
trap 'kill 0' INT TERM
for host in "${hosts[@]}"; do
  dotnet run --no-build --project "$host" 2>&1 | awk -v name="$(basename "$host")" '{ printf "[%s] %s\n", name, $0; fflush() }' &
done

# the back office: node and npm are needed; without them the hosts run without it
if command -v npm >/dev/null 2>&1; then
  (cd samples/edx/app/back-office && { [ -d node_modules ] || npm install --no-audit --no-fund; } && npm start 2>&1 | awk '{ printf "[back-office] %s\n", $0; fflush() }') &
fi
wait
