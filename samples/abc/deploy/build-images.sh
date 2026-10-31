#!/usr/bin/env bash
# Builds the image of every Abc host: abc/<name>:dev. With Docker Desktop's Kubernetes the cluster uses the images of the local Docker, so nothing is pushed anywhere.
# Usage: samples/abc/deploy/build-images.sh [name ...]   (no name: all)
set -euo pipefail
cd "$(dirname "$0")/../../.."
samples/abc/deploy/make-env.sh

# name:path of the host:assembly
hosts=(
    "identity-service:service/identity/Abc.IdentityService.Http.Host:Abc.IdentityService.Http.Host"
    "auth-server-service:app/Abc.AuthServer.Host:Abc.AuthServer.Host"
    "product-service:service/product/Abc.ProductService.Http.Host:Abc.ProductService.Http.Host"
    "shipment-service:service/shipment/Abc.ShipmentService.Http.Host:Abc.ShipmentService.Http.Host"
    "order-service:service/order/Abc.OrderService.Http.Host:Abc.OrderService.Http.Host"
    "gateway-commercial:gateway/Abc.Gateway.Commercial.Host:Abc.Gateway.Commercial.Host"
    "gateway-public:gateway/Abc.Gateway.Public.Host:Abc.Gateway.Public.Host"
)

for entry in "${hosts[@]}"; do
    IFS=: read -r name host assembly <<< "$entry"
    if [ "$#" -gt 0 ] && [[ ! " $* " == *" $name "* ]]; then continue; fi
    echo "== $name"
    docker build --quiet -f samples/abc/deploy/Dockerfile --build-arg "HOST=$host" --build-arg "ASSEMBLY=$assembly" -t "abc/$name:dev" .
done
