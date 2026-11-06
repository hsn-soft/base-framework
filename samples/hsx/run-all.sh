#!/usr/bin/env bash
# Builds and starts the Hsx hosts (identity, content, text normalizer, video generator, auth server, commercial gateway, commercial web) in the development environment. Ctrl+C stops them.
# The identity and content services make their databases (PostgreSQL) and the demo data when they start; the auth server only reads the identity database, so give the identity service a moment first.
# Graylog is off unless PersistentLogger__IsGrayLogActive=true is in the environment (then the logs go to the local infra-graylog).
set -euo pipefail
cd "$(dirname "$0")/../.."

hosts=(
  samples/hsx/service/identity/Hsx.IdentityService.Http.Host
  samples/hsx/service/content/Hsx.ContentService.Http.Host
  samples/hsx/service/text-normalizer/Hsx.TextNormalizerService.Http.Host
  samples/hsx/service/video-generator/Hsx.VideoGeneratorService.Http.Host
  samples/hsx/app/Hsx.AuthServer.Host
  samples/hsx/gateway/Hsx.Gateway.Commercial.Host
  samples/hsx/app/Hsx.CommercialWeb.Host
)

samples/hsx/deploy/ensure-rabbitmq-vhost.sh hsx   # the events of development: the virtual host hsx

for host in "${hosts[@]}"; do
  dotnet build "$host" --verbosity quiet 2>&1 | grep -E "error|rror\(s\)" | head -3 || true
done

export ASPNETCORE_ENVIRONMENT=development
trap 'kill 0' INT TERM
for host in "${hosts[@]}"; do
  dotnet run --no-build --project "$host" 2>&1 | awk -v name="$(basename "$host")" '{ printf "[%s] %s\n", name, $0; fflush() }' &
done
wait
