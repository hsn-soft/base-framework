#!/usr/bin/env bash
# Tries the health of the Edx hosts after deploy.sh. The infrastructure is the one on this machine, shared with everything else you run, so nothing here stops it: a dependency is "taken away" from ONE
# deployment by giving it a wrong address, which is what a broken connection looks like to the platform.
#   samples/edx/deploy/test.sh uat|production     (docker: the gateway and the back office only, there are no probes in Compose)
#   1. every host answers live, startup and ready on both of its pods (the gRPC host on its probe port), and the answer names its checks; the back office is served;
#   2. a rollout of invoice-service with a wrong Redis address: the new pod is alive but not ready and gets no traffic, the two pods that were there keep serving, nothing restarts; rolled back;
#   3. the same with a wrong Kafka address (the broker of the environment is not reached);
#   4. Health:Disabled: with the wrong Redis address and "redis" disabled, the new pod is ready;
#   5. a service is gone (no pod): the gateway is still ready (it looks at itself) and answers the route of the service with a 502.
# It puts everything back as it was.
set -uo pipefail
cd "$(dirname "$0")/../../.."
environment="${1:?docker, uat or production}"

failures=0
check() { # check "what" expected actual
    if [ "$2" = "$3" ]; then echo "  ok    $1 ($3)"; else echo "  FAIL  $1 (expected $2, got $3)"; failures=$((failures + 1)); fi
}

case "$environment" in
    docker) gateway=6500; office=6501 ;;
    uat) namespace=edx-uat; gateway=6600; office=6601 ;;
    production) namespace=edx-prod; gateway=6700; office=6701 ;;
    *) echo "docker, uat or production" >&2; exit 2 ;;
esac

check "gateway ready" 200 "$(curl -s -o /dev/null -w '%{http_code}' --max-time 10 "http://localhost:$gateway/health/ready")"
check "back office is served" 200 "$(curl -s -o /dev/null -w '%{http_code}' --max-time 10 "http://localhost:$office/")"
shown=$(for file in $(curl -s --max-time 10 "http://localhost:$office/" | grep -o '[A-Za-z0-9_.-]*\.js' | sort -u); do curl -s --max-time 10 "http://localhost:$office/$file"; done | grep -c "localhost:$gateway")
check "back office knows the gateway of $environment" yes "$([ "$shown" -gt 0 ] && echo yes || echo no)"
if [ "$environment" = docker ]; then
    [ "$failures" -eq 0 ] && echo "All as expected." || echo "$failures check(s) failed."
    exit "$failures"
fi

k() { kubectl -n "$namespace" "$@"; }
probe_port() { awk -F'|' -v n="$1" '$1==n{print $5}' samples/edx/deploy/hosts.txt; }

forward_pid=""
# what the tests change in invoice-service, taken away again (a test that stopped half way must not leave it changed)
reset_invoice_service() {
    k set env deployment/invoice-service ConnectionStrings__Redis- Kafka__BootstrapServers- Health__Disabled__0- >/dev/null 2>&1
    k scale deployment/invoice-service --replicas=2 >/dev/null 2>&1
    k rollout status deployment/invoice-service --timeout=300s >/dev/null 2>&1
}
cleanup() {
    [ -n "$forward_pid" ] && kill "$forward_pid" 2>/dev/null
    reset_invoice_service
}
trap cleanup EXIT
reset_invoice_service

# code and body of an endpoint of one pod, through a port-forward to it (kubectl itself is run in the background, not the function k: killing a function's subshell would leave the port-forward running)
call() { # call pod port path
    local port=$((18000 + RANDOM % 1000))
    kubectl -n "$namespace" port-forward "pod/$1" "$port:$2" >/dev/null 2>&1 & forward_pid=$!
    for _ in $(seq 1 20); do curl -s -o /dev/null --max-time 1 "http://localhost:$port$3" 2>/dev/null && break; sleep 0.3; done
    curl -s --max-time 10 -w '\n%{http_code}' "http://localhost:$port$3"
    kill "$forward_pid" 2>/dev/null; wait "$forward_pid" 2>/dev/null; forward_pid=""
}
code() { call "$1" "$2" "$3" | tail -n 1; }
pods() { k get pods -l "app=$1" --field-selector=status.phase=Running -o jsonpath='{.items[*].metadata.name}'; }
ready_count() { k get deployment "$1" -o jsonpath='{.status.readyReplicas}'; }
restarts() { k get pods -l "app=$1" -o jsonpath='{range .items[*]}{.status.containerStatuses[0].restartCount}{"\n"}{end}' | sort -n | tail -n 1; }
new_pod() { # the pod of a deployment that is running but not ready (the one of the rollout that stalls)
    k get pods -l "app=$1" -o jsonpath='{range .items[*]}{.metadata.name}{" "}{.status.containerStatuses[0].ready}{" "}{.status.containerStatuses[0].state.running.startedAt}{"\n"}{end}' | awk '$2=="false" && $3!=""{print $1}' | head -n 1
}
waits_for_new_pod() { for _ in $(seq 1 40); do [ -n "$(new_pod "$1")" ] && return 0; sleep 2; done; return 1; }

echo "== 1. the endpoints of every pod ($namespace)"
for service in identity-service auth-server-service invoice-service signer-service integration-firm-http integration-firm-grpc integration-firm-wcf integration-gib-http integration-gib-wcf mock-center gateway back-office; do
    port=$(probe_port "$service")
    for pod in $(pods "$service"); do
        for path in live startup ready; do check "$pod /health/$path" 200 "$(code "$pod" "$port" "/health/$path")"; done
    done
    first=$(pods "$service" | awk '{print $1}')
    [ "$service" = back-office ] || echo "      $service: $(call "$first" "$port" /health/ready | head -n 1 | python3 -c 'import json,sys; d=json.load(sys.stdin); print(d["status"], [c["name"] for c in d["checks"]])')"
done

stalled_rollout() { # stalled_rollout "what" ENV=value ...
    local what="$1"; shift
    k set env deployment/invoice-service "$@" >/dev/null
    waits_for_new_pod invoice-service
    local pod; pod=$(new_pod invoice-service)
    check "$what: the new pod is not ready" 503 "$(code "$pod" 8080 /health/ready)"
    check "$what: the new pod is alive" 200 "$(code "$pod" 8080 /health/live)"
    sleep 12
    check "$what: the two pods that were there keep serving" 2 "$(ready_count invoice-service)"
    check "$what: nothing restarted" 0 "$(restarts invoice-service)"
    echo "      $(call "$pod" 8080 /health/ready | head -n 1 | cut -c1-230)"
}
rolls_back() {
    reset_invoice_service
    check "invoice-service back, two pods ready" 2 "$(ready_count invoice-service)"
}

echo "== 2. invoice-service with a wrong Redis address"
stalled_rollout "wrong Redis" ConnectionStrings__Redis=host.docker.internal:6399
rolls_back

echo "== 3. invoice-service with a wrong Kafka address"
stalled_rollout "wrong Kafka" Kafka__BootstrapServers=host.docker.internal:9999
rolls_back

echo "== 4. Health:Disabled: the wrong Redis address does not matter to a service that does not check Redis"
k set env deployment/invoice-service ConnectionStrings__Redis=host.docker.internal:6399 Health__Disabled__0=redis >/dev/null
k rollout status deployment/invoice-service --timeout=300s >/dev/null
check "invoice-service ready with a wrong Redis address, redis disabled" 2 "$(ready_count invoice-service)"
rolls_back

echo "== 5. a service is gone: the gateway"
k scale deployment/invoice-service --replicas=0 >/dev/null
k wait --for=delete pod -l app=invoice-service --timeout=120s >/dev/null 2>&1
check "gateway ready" 200 "$(curl -s -o /dev/null -w '%{http_code}' --max-time 10 "http://localhost:$gateway/health/ready")"
check "route of the service" 502 "$(curl -s -o /dev/null -w '%{http_code}' -X POST --max-time 15 "http://localhost:$gateway/invoice-service/v1/invoices/paged-list")"
k scale deployment/invoice-service --replicas=2 >/dev/null
k rollout status deployment/invoice-service --timeout=300s >/dev/null

echo
[ "$failures" -eq 0 ] && echo "All as expected." || echo "$failures check(s) failed."
exit "$failures"
