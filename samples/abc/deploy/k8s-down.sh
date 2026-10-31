#!/usr/bin/env bash
# Takes the Abc system out of the cluster: the namespace, with everything in it (the data of the infrastructure too: it has no volume).
set -euo pipefail
kubectl delete namespace abc --ignore-not-found
