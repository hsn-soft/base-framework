#!/usr/bin/env bash
# End-to-end check of the whole flow through the gateway. Start the infrastructure and the six services first
# (see samples/README.md), then run:   ./samples/smoke-test.sh [gateway-url]
set -euo pipefail

GATEWAY="${1:-http://localhost:5100}"
KEYBOARD="11111111-1111-1111-1111-111111111111"   # stock 100
MOUSE="22222222-2222-2222-2222-222222222222"      # stock 3

failures=0
pass() { echo "  PASS  $1"; }
fail() { echo "  FAIL  $1"; failures=$((failures + 1)); }
check() { if [ "$2" = "$3" ]; then pass "$1"; else fail "$1 (expected '$3', got '$2')"; fi; }

# json <path> reads a value from JSON on stdin: json "a.b" -> value
json() { python3 -c "import json,sys; d=json.load(sys.stdin); [d:=d[k] if isinstance(d,dict) else d[int(k)] for k in sys.argv[1].split('.')]; print('' if d is None else d)" "$1"; }
status() { curl -s -o /dev/null -w '%{http_code}' "$@"; }

echo "Gateway: $GATEWAY"

echo "1. Authentication"
check "orders need a token" "$(status "$GATEWAY/api/orders")" "401"
alice="$(curl -s -X POST "$GATEWAY/api/auth/login" -H 'Content-Type: application/json' -d '{"username":"alice","password":"alice123"}' | json accessToken)"
admin="$(curl -s -X POST "$GATEWAY/api/auth/login" -H 'Content-Type: application/json' -d '{"username":"admin","password":"admin123"}' | json accessToken)"
check "wrong password is rejected" "$(status -X POST "$GATEWAY/api/auth/login" -H 'Content-Type: application/json' -d '{"username":"alice","password":"nope"}')" "401"
[ -n "$alice" ] && pass "alice got a token" || fail "alice got a token"

echo "2. Order with enough stock: order -> stock -> shipment -> order"
key="smoke-$(date +%s)"
order="$(curl -s -X POST "$GATEWAY/api/orders" -H "Authorization: Bearer $alice" -H "Idempotency-Key: $key" -H 'Content-Type: application/json' -d "{\"productId\":\"$KEYBOARD\",\"quantity\":2}")"
order_id="$(echo "$order" | json id)"
check "a new order starts as Pending" "$(echo "$order" | json status)" "Pending"
again="$(curl -s -X POST "$GATEWAY/api/orders" -H "Authorization: Bearer $alice" -H "Idempotency-Key: $key" -H 'Content-Type: application/json' -d "{\"productId\":\"$KEYBOARD\",\"quantity\":2}" | json id)"
check "the same Idempotency-Key returns the same order" "$again" "$order_id"

final=""
for _ in $(seq 1 40); do
    final="$(curl -s "$GATEWAY/api/orders/$order_id" -H "Authorization: Bearer $alice")"
    [ "$(echo "$final" | json status)" = "Shipped" ] && break
    sleep 0.5
done
check "the order ends as Shipped" "$(echo "$final" | json status)" "Shipped"
check "total price = 2 x 89.90" "$(echo "$final" | json totalPrice)" "179.8"
tracking="$(echo "$final" | json trackingNumber)"
[ -n "$tracking" ] && pass "tracking number $tracking" || fail "tracking number is set"
check "the shipment service knows the same tracking number" \
    "$(curl -s "$GATEWAY/api/shipments/by-order/$order_id" -H "Authorization: Bearer $alice" | json trackingNumber)" "$tracking"

echo "3. Order without enough stock"
reject="$(curl -s -X POST "$GATEWAY/api/orders" -H "Authorization: Bearer $alice" -H 'Content-Type: application/json' -d "{\"productId\":\"$MOUSE\",\"quantity\":50}" | json id)"
final=""
for _ in $(seq 1 40); do
    final="$(curl -s "$GATEWAY/api/orders/$reject" -H "Authorization: Bearer $alice")"
    [ "$(echo "$final" | json status)" = "Rejected" ] && break
    sleep 0.5
done
check "the order ends as Rejected" "$(echo "$final" | json status)" "Rejected"
check "with the reason" "$(echo "$final" | json rejectReason)" "Not enough stock."

echo "4. Authorization"
check "a customer cannot create products" "$(status -X POST "$GATEWAY/api/products" -H "Authorization: Bearer $alice" -H 'Content-Type: application/json' -d '{"name":"X","sku":"X-1","unitPrice":1,"stock":1}')" "403"
check "an admin can" "$(status -X POST "$GATEWAY/api/products" -H "Authorization: Bearer $admin" -H 'Content-Type: application/json' -d "{\"name\":\"Cable\",\"sku\":\"CAB-$(date +%s)\",\"unitPrice\":5,\"stock\":10}")" "201"
check "an unknown order is 404" "$(status "$GATEWAY/api/orders/00000000-0000-0000-0000-000000000000" -H "Authorization: Bearer $alice")" "404"

echo
if [ "$failures" -eq 0 ]; then echo "ALL CHECKS PASSED"; else echo "$failures CHECK(S) FAILED"; exit 1; fi
