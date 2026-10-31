#!/usr/bin/env bash
# Builds the image of every Abc host (the list is hosts.txt): abc/<name>:dev. With Docker Desktop's Kubernetes the cluster uses the images of the local Docker, so nothing is pushed anywhere.
# Usage: samples/abc/deploy/build-images.sh [name ...]   (no name: all)
set -euo pipefail
cd "$(dirname "$0")/../../.."
samples/abc/deploy/make-env.sh

while IFS='|' read -r name folder assembly _; do
    case "$name" in ''|'#'*) continue ;; esac
    if [ "$#" -gt 0 ] && [[ ! " $* " == *" $name "* ]]; then continue; fi
    echo "== $name"
    docker build --quiet -f samples/abc/deploy/Dockerfile --build-arg "HOST=$folder" --build-arg "ASSEMBLY=$assembly" -t "abc/$name:dev" .
done < samples/abc/deploy/hosts.txt
