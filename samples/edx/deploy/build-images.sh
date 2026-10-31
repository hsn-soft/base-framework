#!/usr/bin/env bash
# Builds the image of every Edx host (the list is hosts.txt): edx/<name>:dev. The back office is built once for each environment (`ng build --configuration <environment>`, the address of the gateway
# of the environment is in the build) and its images are edx/back-office:docker, :uat and :production. With Docker Desktop's Kubernetes the cluster uses the images of the local Docker.
# Usage: samples/edx/deploy/build-images.sh [name ...]   (no name: all)
set -euo pipefail
cd "$(dirname "$0")/../../.."
samples/edx/deploy/make-env.sh

while IFS='|' read -r name folder assembly kind _; do
    case "$name" in ''|'#'*) continue ;; esac
    if [ "$#" -gt 0 ] && [[ ! " $* " == *" $name "* ]]; then continue; fi
    echo "== $name"
    if [ "$kind" = spa ]; then
        (cd samples/edx/app/back-office && { [ -d node_modules ] || npm install --no-audit --no-fund; })
        for environment in docker uat production; do
            (cd samples/edx/app/back-office && npx ng build --configuration "$environment" >/dev/null)
            context="$(mktemp -d)"
            cp samples/edx/deploy/nginx.conf "$context/nginx.conf"
            cp -R samples/edx/app/back-office/dist/back-office/browser "$context/site"
            docker build --quiet -f samples/edx/deploy/Dockerfile.back-office -t "edx/$name:$environment" "$context"
            rm -rf "${context:?}"
        done
    else
        docker build --quiet -f samples/edx/deploy/Dockerfile --build-arg "HOST=$folder" --build-arg "ASSEMBLY=$assembly" -t "edx/$name:dev" .
    fi
done < samples/edx/deploy/hosts.txt
