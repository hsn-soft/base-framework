#!/usr/bin/env bash
# Deploys the Edx system to an environment on this machine, from the source:
#   samples/edx/deploy/deploy.sh docker       Docker Compose, the environment `docker` (databases Edx_*_Dev, the default Kafka)
#   samples/edx/deploy/deploy.sh uat          Kubernetes of Docker Desktop, namespace edx-uat, two pods of each host (databases Edx_*_Uat, the Kafka of uat)
#   samples/edx/deploy/deploy.sh production   Kubernetes of Docker Desktop, namespace edx-prod, two pods of each host (databases Edx_*_Prod, the Kafka of production)
# Add --no-build to use the images that are there. What it does: the secrets (make-env.sh), the images (build-images.sh), the settings of the environment with the secrets put in (render-settings.sh), then the
# deployment. The infrastructure is the one on this machine, so it has to be running (SQL Server, MongoDB, Redis, MinIO and the Kafka of the environment: infra-kafka, infra-kafka-uat, infra-kafka-prod).
# The ports on localhost are in hosts.txt (docker 65xx, uat 66xx, production 67xx): gateway 00, back office 01, firm invoice REST 10, gRPC 11, WCF 12, mock center 20, GIB REST 40.
# This machine does not hold the uat and the production of both samples at once: take one away (undeploy.sh) before the other is deployed.
set -euo pipefail
cd "$(dirname "$0")/../../.."
environment="${1:?docker, uat or production}"
build=true; [ "${2:-}" = "--no-build" ] && build=false

case "$environment" in
    docker) index=1 ;;
    uat) namespace=edx-uat; index=2 ;;
    production) namespace=edx-prod; index=3 ;;
    *) echo "unknown environment '$environment' (docker, uat or production)" >&2; exit 2 ;;
esac

samples/edx/deploy/make-env.sh
$build && samples/edx/deploy/build-images.sh
samples/edx/deploy/render-settings.sh "$environment"

if [ "$environment" = docker ]; then
    docker compose -f samples/edx/deploy/docker-compose.yml up -d --force-recreate
    echo "Edx is up in Docker: gateway http://localhost:6500, back office http://localhost:6501"
    exit 0
fi

kubectl create namespace "$namespace" --dry-run=client -o yaml | kubectl apply -f -
kubectl -n "$namespace" create secret generic edx-auth-private-key --from-file=private_key.xml=samples/edx/app/Edx.AuthServer.Host/private_key.xml --dry-run=client -o yaml | kubectl apply -f -

substitutions=(-e "s/__ENVIRONMENT__/$environment/g" -e "s/__REPLICAS__/2/g")
while IFS='|' read -r name _ _ kind _ ports; do
    case "$name" in ''|'#'*) continue ;; esac
    if [ "$kind" != spa ]; then
        kubectl -n "$namespace" create secret generic "settings-$name" --from-file="appsettings.$environment.json=samples/edx/deploy/.rendered/$environment/$name.json" --dry-run=client -o yaml | kubectl apply -f -
    fi
    if [ "$ports" != "-" ]; then
        token="__PORT_$(echo "$name" | tr 'a-z-' 'A-Z_')__"
        substitutions+=(-e "s/$token/$(echo "$ports" | cut -d, -f$((index)))/g")
    fi
done < samples/edx/deploy/hosts.txt

existing=$(kubectl -n "$namespace" get deployment -o name | wc -l)
sed "${substitutions[@]}" samples/edx/deploy/k8s/apps.yaml | kubectl -n "$namespace" apply -f -

# a changed secret does not restart a pod: the pods are replaced when the settings or the images changed
[ "$existing" -gt 0 ] && kubectl -n "$namespace" rollout restart deployment >/dev/null
while IFS='|' read -r name _; do
    case "$name" in ''|'#'*) continue ;; esac
    kubectl -n "$namespace" rollout status "deployment/$name" --timeout=300s
done < samples/edx/deploy/hosts.txt
kubectl -n "$namespace" get pods
echo "Edx is up in $namespace: gateway http://localhost:$(( 6500 + (index - 1) * 100 )), back office http://localhost:$(( 6501 + (index - 1) * 100 ))"
