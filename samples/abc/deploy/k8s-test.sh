#!/usr/bin/env bash
# Tries the health endpoints of the Abc hosts in the cluster (after k8s-up.sh), then takes dependencies away one by one and checks what the platform sees:
#   1. every host answers live, startup and ready, and the answer names its checks;
#   2. Redis is gone: the services that wait for it are not ready (out of the traffic) but alive (not restarted), and are ready again when it is back;
#   3. RabbitMQ is gone: the same, for the services with an event bus;
#   4. Health:Disabled (here by an environment variable of the pod): a service that does not check Redis stays ready without it;
#   5. a service is gone: the gateway is still ready (it looks at itself) and answers the route of the service with a 502.
# It puts everything back as it was.
set -uo pipefail
cd "$(dirname "$0")/../../.."

failures=0
check() { # check "what" expected actual
    if [ "$2" = "$3" ]; then echo "  ok    $1 ($3)"; else echo "  FAIL  $1 (expected $2, got $3)"; failures=$((failures + 1)); fi
}

forward_pid=""
cleanup() { [ -n "$forward_pid" ] && kill "$forward_pid" 2>/dev/null; kubectl -n abc scale deployment/redis deployment/rabbitmq deployment/order-service --replicas=1 >/dev/null 2>&1; kubectl -n abc set env deployment/shipment-service Health__Disabled__0- >/dev/null 2>&1; }
trap cleanup EXIT

# code and body of an endpoint of a host, through a port-forward to its service
call() { # call service path
    kubectl -n abc port-forward "svc/$1" 18080:8080 >/dev/null 2>&1 & forward_pid=$!
    for _ in $(seq 1 20); do curl -s -o /dev/null --max-time 1 "http://localhost:18080$2" 2>/dev/null && break; sleep 0.3; done
    curl -s --max-time 10 -w '\n%{http_code}' "http://localhost:18080$2"
    kill "$forward_pid" 2>/dev/null; wait "$forward_pid" 2>/dev/null; forward_pid=""
}
code() { call "$1" "$2" | tail -n 1; }

# waits until a call gives the code, at most ~60 s
waits_for() { # waits_for service path code
    for _ in $(seq 1 30); do [ "$(code "$1" "$2")" = "$3" ] && return 0; sleep 2; done
    return 1
}
restarts() { kubectl -n abc get pods -l "app=$1" -o jsonpath='{.items[0].status.containerStatuses[0].restartCount}'; }
# the addresses of the pods of a service that are ready, i.e. the ones it sends traffic to
endpoints() { kubectl -n abc get endpointslices -l "kubernetes.io/service-name=$1" -o jsonpath='{.items[*].endpoints[?(@.conditions.ready==true)].addresses[0]}'; }
# the platform takes a pod out of the traffic after the failures of its readiness probe (here 2 x 5 s)
leaves_traffic() { for _ in $(seq 1 30); do [ -z "$(endpoints "$1")" ] && return 0; sleep 2; done; return 1; }

echo "== 1. the endpoints of every host"
for service in identity-service auth-server-service product-service shipment-service order-service; do
    for path in live startup ready; do check "$service /health/$path" 200 "$(code "$service" "/health/$path")"; done
    echo "      $(call "$service" /health/ready | head -n 1 | python3 -c 'import json,sys; d=json.load(sys.stdin); print(d["status"], [c["name"] for c in d["checks"]])')"
done

echo "== 2. Redis is gone"
kubectl -n abc scale deployment/redis --replicas=0 >/dev/null
waits_for order-service /health/ready 503; check "order-service ready" 503 "$(code order-service /health/ready)"
check "order-service live" 200 "$(code order-service /health/live)"
leaves_traffic order-service; check "order-service out of the traffic" "" "$(endpoints order-service)"
echo "      $(call order-service /health/ready | head -n 1)"
kubectl -n abc scale deployment/redis --replicas=1 >/dev/null
kubectl -n abc rollout status deployment/redis --timeout=120s >/dev/null
waits_for order-service /health/ready 200; check "order-service ready again" 200 "$(code order-service /health/ready)"
check "order-service not restarted" 0 "$(restarts order-service)"

echo "== 3. RabbitMQ is gone"
kubectl -n abc scale deployment/rabbitmq --replicas=0 >/dev/null
waits_for order-service /health/ready 503; check "order-service ready" 503 "$(code order-service /health/ready)"
check "order-service live" 200 "$(code order-service /health/live)"
echo "      $(call order-service /health/ready | head -n 1)"
kubectl -n abc scale deployment/rabbitmq --replicas=1 >/dev/null
kubectl -n abc rollout status deployment/rabbitmq --timeout=180s >/dev/null
waits_for order-service /health/ready 200; check "order-service ready again" 200 "$(code order-service /health/ready)"
check "order-service not restarted" 0 "$(restarts order-service)"

echo "== 4. Health:Disabled: shipment-service does not wait for Redis"
kubectl -n abc set env deployment/shipment-service Health__Disabled__0=redis >/dev/null
kubectl -n abc rollout status deployment/shipment-service --timeout=300s >/dev/null
kubectl -n abc scale deployment/redis --replicas=0 >/dev/null
sleep 15
check "shipment-service ready without Redis" 200 "$(code shipment-service /health/ready)"
check "order-service (checks Redis) not ready" 503 "$(code order-service /health/ready)"
kubectl -n abc scale deployment/redis --replicas=1 >/dev/null
kubectl -n abc rollout status deployment/redis --timeout=120s >/dev/null
kubectl -n abc set env deployment/shipment-service Health__Disabled__0- >/dev/null
waits_for order-service /health/ready 200

echo "== 5. a service is gone: the gateway"
# the commercial gateway waits for Redis too (it was away a moment ago): it is back when the platform has seen its probe pass
for _ in $(seq 1 40); do [ "$(curl -s -o /dev/null -w '%{http_code}' --max-time 3 http://localhost:5500/health/ready)" = "200" ] && break; sleep 2; done
kubectl -n abc scale deployment/order-service --replicas=0 >/dev/null
leaves_traffic order-service
# a pod that is stopping still serves the connections the gateway has open to it until it is gone; the 502 is for when it is
kubectl -n abc wait --for=delete pod -l app=order-service --timeout=120s >/dev/null 2>&1
check "gateway-commercial ready" 200 "$(curl -s -o /dev/null -w '%{http_code}' --max-time 10 http://localhost:5500/health/ready)"
check "route of the service" 502 "$(curl -s -o /dev/null -w '%{http_code}' -X POST --max-time 15 http://localhost:5500/order-service/v1/orders/paged-list)"
kubectl -n abc scale deployment/order-service --replicas=1 >/dev/null
kubectl -n abc rollout status deployment/order-service --timeout=300s >/dev/null

echo
[ "$failures" -eq 0 ] && echo "All as expected." || echo "$failures check(s) failed."
exit "$failures"
