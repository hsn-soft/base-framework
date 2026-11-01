#!/usr/bin/env bash
# Builds the image of every Hsx host (the list is hosts.txt): hsx/<name>:dev. With Docker Desktop's Kubernetes the cluster uses the images of the local Docker, so nothing is pushed anywhere.
# Usage: samples/hsx/deploy/build-images.sh [name ...]   (no name: all)
set -euo pipefail
cd "$(dirname "$0")/../../.."
samples/hsx/deploy/make-env.sh

while IFS='|' read -r name folder assembly _; do
    case "$name" in ''|'#'*) continue ;; esac
    if [ "$#" -gt 0 ] && [[ ! " $* " == *" $name "* ]]; then continue; fi
    echo "== $name"
    docker build --quiet -f samples/hsx/deploy/Dockerfile --build-arg "HOST=$folder" --build-arg "ASSEMBLY=$assembly" -t "hsx/$name:dev" .
done < samples/hsx/deploy/hosts.txt
