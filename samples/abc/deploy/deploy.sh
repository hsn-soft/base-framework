#!/usr/bin/env bash
# Deploys the Abc system to an environment on this machine, from the source:
#   samples/abc/deploy/deploy.sh docker       Docker Compose, the environment `docker` (databases Abc_*_Dev)
#   samples/abc/deploy/deploy.sh uat          Kubernetes of Docker Desktop, namespace abc-uat, two pods of each host (databases Abc_*_Uat)
#   samples/abc/deploy/deploy.sh production   Kubernetes of Docker Desktop, namespace abc-prod, two pods of each host (databases Abc_*_Prod)
# Add --no-build to use the images that are there. What it does: the secrets (make-env.sh), the images (build-images.sh), the settings of the environment with the passwords put in (render-settings.sh), then the
# deployment. The infrastructure is the one on this machine, so it has to be running (PostgreSQL, SQL Server, MongoDB, Redis, RabbitMQ).
# Ports on localhost (commercial gateway, public gateway, commercial web, public web): docker 5500 5600 5700 5800; uat 5510 5610 5710 5810; production 5520 5620 5720 5820.
set -euo pipefail
cd "$(dirname "$0")/../../.."
environment="${1:?docker, uat or production}"
build=true; [ "${2:-}" = "--no-build" ] && build=false

case "$environment" in
    docker) ;;
    uat) namespace=abc-uat; ports=(5510 5610 5710 5810) ;;
    production) namespace=abc-prod; ports=(5520 5620 5720 5820) ;;
    *) echo "unknown environment '$environment' (docker, uat or production)" >&2; exit 2 ;;
esac

samples/abc/deploy/make-env.sh
$build && samples/abc/deploy/build-images.sh
samples/abc/deploy/render-settings.sh "$environment"
# the events of an environment are on a virtual host of their own (the default one, uat, prod)
[ "$environment" != docker ] && samples/abc/deploy/ensure-rabbitmq-vhost.sh "${environment/production/prod}"

if [ "$environment" = docker ]; then
    docker compose -f samples/abc/deploy/docker-compose.yml up -d --force-recreate
    echo "Abc is up in Docker: commercial gateway http://localhost:5500, public gateway http://localhost:5600, commercial web http://localhost:5700, public web http://localhost:5800"
    exit 0
fi

kubectl create namespace "$namespace" --dry-run=client -o yaml | kubectl apply -f -
kubectl -n "$namespace" create secret generic abc-auth-private-key --from-file=private_key.xml=samples/abc/app/Abc.AuthServer.Host/private_key.xml --dry-run=client -o yaml | kubectl apply -f -
while IFS='|' read -r name _; do
    case "$name" in ''|'#'*) continue ;; esac
    kubectl -n "$namespace" create secret generic "settings-$name" --from-file="appsettings.$environment.json=samples/abc/deploy/.rendered/$environment/$name.json" --dry-run=client -o yaml | kubectl apply -f -
done < samples/abc/deploy/hosts.txt

existing=$(kubectl -n "$namespace" get deployment -o name | wc -l)
sed -e "s/__ENVIRONMENT__/$environment/g" -e "s/__REPLICAS__/2/g" \
    -e "s/__PORT_GATEWAY_COMMERCIAL__/${ports[0]}/g" -e "s/__PORT_GATEWAY_PUBLIC__/${ports[1]}/g" \
    -e "s/__PORT_COMMERCIAL_WEB__/${ports[2]}/g" -e "s/__PORT_PUBLIC_WEB__/${ports[3]}/g" \
    samples/abc/deploy/k8s/apps.yaml | kubectl -n "$namespace" apply -f -

# a changed secret does not restart a pod: the pods are replaced when the settings or the images changed
[ "$existing" -gt 0 ] && kubectl -n "$namespace" rollout restart deployment >/dev/null
while IFS='|' read -r name _; do
    case "$name" in ''|'#'*) continue ;; esac
    kubectl -n "$namespace" rollout status "deployment/$name" --timeout=300s
done < samples/abc/deploy/hosts.txt
kubectl -n "$namespace" get pods
echo "Abc is up in $namespace: commercial gateway http://localhost:${ports[0]}, public gateway http://localhost:${ports[1]}, commercial web http://localhost:${ports[2]}, public web http://localhost:${ports[3]}"
