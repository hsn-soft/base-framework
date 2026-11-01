#!/usr/bin/env bash
# Deploys the Hsx system to an environment on this machine, from the source:
#   samples/hsx/deploy/deploy.sh docker       Docker Compose, the environment `docker` (databases Hsx_*_Dev)
#   samples/hsx/deploy/deploy.sh uat          Kubernetes of Docker Desktop, namespace hsx-uat, two pods of each host (databases Hsx_*_Uat)
#   samples/hsx/deploy/deploy.sh production   Kubernetes of Docker Desktop, namespace hsx-prod, two pods of each host (databases Hsx_*_Prod)
# Add --no-build to use the images that are there. What it does: the secrets (make-env.sh), the images (build-images.sh), the settings of the environment with the passwords put in (render-settings.sh), then the
# deployment. The infrastructure is the one on this machine, so it has to be running (PostgreSQL, MongoDB, Redis, RabbitMQ).
# Ports on localhost (commercial gateway, commercial web): development 7200 7300; docker 7200 7300 (the same: stop the development hosts first); uat 7210 7310; production 7220 7320.
set -euo pipefail
cd "$(dirname "$0")/../../.."
environment="${1:?docker, uat or production}"
build=true; [ "${2:-}" = "--no-build" ] && build=false

case "$environment" in
    docker) ;;
    uat) namespace=hsx-uat; ports=(7210 7310) ;;
    production) namespace=hsx-prod; ports=(7220 7320) ;;
    *) echo "unknown environment '$environment' (docker, uat or production)" >&2; exit 2 ;;
esac

samples/hsx/deploy/make-env.sh
$build && samples/hsx/deploy/build-images.sh
samples/hsx/deploy/render-settings.sh "$environment"
# the events of an environment are on a virtual host of their own (hsx for development and docker, hsx-uat, hsx-prod)
samples/hsx/deploy/ensure-rabbitmq-vhost.sh "$(case "$environment" in docker) echo hsx ;; uat) echo hsx-uat ;; production) echo hsx-prod ;; esac)"

if [ "$environment" = docker ]; then
    docker compose -f samples/hsx/deploy/docker-compose.yml up -d --force-recreate
    echo "Hsx is up in Docker: commercial gateway http://localhost:7200, commercial web http://localhost:7300"
    exit 0
fi

kubectl create namespace "$namespace" --dry-run=client -o yaml | kubectl apply -f -
kubectl -n "$namespace" create secret generic hsx-auth-private-key --from-file=private_key.xml=samples/hsx/app/Hsx.AuthServer.Host/private_key.xml --dry-run=client -o yaml | kubectl apply -f -
while IFS='|' read -r name _; do
    case "$name" in ''|'#'*) continue ;; esac
    kubectl -n "$namespace" create secret generic "settings-$name" --from-file="appsettings.$environment.json=samples/hsx/deploy/.rendered/$environment/$name.json" --dry-run=client -o yaml | kubectl apply -f -
done < samples/hsx/deploy/hosts.txt

existing=$(kubectl -n "$namespace" get deployment -o name | wc -l)
sed -e "s/__ENVIRONMENT__/$environment/g" -e "s/__REPLICAS__/2/g" \
    -e "s/__PORT_GATEWAY_COMMERCIAL__/${ports[0]}/g" -e "s/__PORT_COMMERCIAL_WEB__/${ports[1]}/g" \
    samples/hsx/deploy/k8s/apps.yaml | kubectl -n "$namespace" apply -f -

# a changed secret does not restart a pod: the pods are replaced when the settings or the images changed
[ "$existing" -gt 0 ] && kubectl -n "$namespace" rollout restart deployment >/dev/null
while IFS='|' read -r name _; do
    case "$name" in ''|'#'*) continue ;; esac
    kubectl -n "$namespace" rollout status "deployment/$name" --timeout=300s
done < samples/hsx/deploy/hosts.txt
kubectl -n "$namespace" get pods
echo "Hsx is up in $namespace: commercial gateway http://localhost:${ports[0]}, commercial web http://localhost:${ports[1]}"
