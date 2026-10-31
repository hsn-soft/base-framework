#!/usr/bin/env bash
# Puts the Abc system into the cluster of the current kubectl context (Docker Desktop's Kubernetes: the images of build-images.sh are the ones of the local Docker).
# The secrets are made here from deploy/.env and the private key of the auth server; no manifest has a password.
#   samples/abc/deploy/build-images.sh && samples/abc/deploy/k8s-up.sh
# The commercial gateway is then on localhost:5500, the public one on localhost:5600.
set -euo pipefail
cd "$(dirname "$0")/../../.."
samples/abc/deploy/make-env.sh

kubectl apply -f samples/abc/deploy/k8s/namespace.yaml
kubectl -n abc create secret generic abc-secrets --from-env-file=samples/abc/deploy/.env --dry-run=client -o yaml | kubectl apply -f -
kubectl -n abc create secret generic abc-auth-private-key --from-file=private_key.xml=samples/abc/app/Abc.AuthServer.Host/private_key.xml --dry-run=client -o yaml | kubectl apply -f -
kubectl apply -k samples/abc/deploy/k8s

for deployment in postgres sqlserver mongo redis rabbitmq identity-service auth-server-service product-service shipment-service order-service gateway-commercial gateway-public; do
    kubectl -n abc rollout status "deployment/$deployment" --timeout=300s
done
kubectl -n abc get pods
