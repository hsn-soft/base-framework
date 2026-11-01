#!/usr/bin/env bash
# Takes the Edx system out of an environment: docker (Compose), uat or production (the namespace, with everything in it). The databases and the buckets stay: they are on the infrastructure of this machine.
# Usage: samples/edx/deploy/undeploy.sh docker|uat|production
set -euo pipefail
cd "$(dirname "$0")/../../.."
case "${1:?docker, uat or production}" in
    docker) docker compose -f samples/edx/deploy/docker-compose.yml down ;;
    uat) kubectl delete namespace edx-uat --ignore-not-found ;;
    production) kubectl delete namespace edx-prod --ignore-not-found ;;
    *) echo "unknown environment '$1' (docker, uat or production)" >&2; exit 2 ;;
esac
