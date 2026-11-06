#!/usr/bin/env bash
# Takes the Abc system out of an environment: docker (Compose), uat or production (the namespace, with everything in it). The databases stay: they are on the infrastructure of this machine.
# Usage: samples/abc/deploy/undeploy.sh docker|uat|production
set -euo pipefail
cd "$(dirname "$0")/../../.."
case "${1:?docker, uat or production}" in
    docker) docker compose -f samples/abc/deploy/docker-compose.yml down ;;
    uat) kubectl delete namespace abc-uat --ignore-not-found ;;
    production) kubectl delete namespace abc-prod --ignore-not-found ;;
    *) echo "unknown environment '$1' (docker, uat or production)" >&2; exit 2 ;;
esac
