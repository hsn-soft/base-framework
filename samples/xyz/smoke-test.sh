#!/usr/bin/env bash
# End-to-end check of the whole flow through the commercial gateway. Start the infrastructure (PostgreSQL, Redis, Kafka) and the hosts first (./samples/xyz/run-all.sh), then run:
#   ./samples/xyz/smoke-test.sh [gateway-url]
set -euo pipefail

GATEWAY="${1:-http://localhost:5100}"

failures=0
pass() { echo "  PASS  $1"; }
fail() { echo "  FAIL  $1"; failures=$((failures + 1)); }
check() { if [ "$2" = "$3" ]; then pass "$1"; else fail "$1 (expected '$3', got '$2')"; fi; }

# json <path> reads a value from JSON on stdin: json "payload.items.0.id" -> value
json() { python3 -c "import json,sys; d=json.load(sys.stdin); [d:=d[k] if isinstance(d,dict) else d[int(k)] for k in sys.argv[1].split('.')]; print('' if d is None else d)" "$1"; }
status() { curl -s -o /dev/null -w '%{http_code}' "$@"; }
empty='{}'
post() { curl -s -X POST "$GATEWAY/$1" -H "Authorization: Bearer ${2:-}" -H 'Content-Type: application/json' -d "${3:-$empty}"; }
login() { curl -s -X POST "$GATEWAY/auth-server/v1/auth/login" -H 'Content-Type: application/json' -d "{\"username\":\"$1\",\"password\":\"$2\"}"; }

# an order for one product (the first with this sku), then wait until it is Delivered or Cancelled; prints the order
place_and_wait() {
    local address="$1" sku="$2" quantity="$3"
    local product product_id name price order id final
    product="$(post product-service/v1/products/filter-list "$alice" "{\"sku\":\"$sku\"}")"
    product_id="$(echo "$product" | json payload.0.id)"; name="$(echo "$product" | json payload.0.name)"; price="$(echo "$product" | json payload.0.price)"
    order="$(post order-service/v1/orders/create "$alice" "{\"shippingAddress\":\"$address\",\"lines\":[{\"productId\":\"$product_id\",\"productName\":\"$name\",\"unitPrice\":$price,\"quantity\":$quantity}]}")"
    id="$(echo "$order" | json payload.id)"
    for _ in $(seq 1 60); do
        final="$(post order-service/v1/orders/get/"$id" "$alice")"
        case "$(echo "$final" | json payload.status)" in Delivered | Cancelled) echo "$final"; return ;; esac
        sleep 0.5
    done
    echo "$final"
}

echo "Gateway: $GATEWAY"

echo "1. Authentication"
check "orders need a token" "$(status -X POST "$GATEWAY/order-service/v1/orders/paged-list" -H 'Content-Type: application/json' -d '{}')" "401"
login alice alice123 > /tmp/xyz-alice.json
alice="$(json payload.accessToken < /tmp/xyz-alice.json)"
refresh="$(json payload.refreshToken < /tmp/xyz-alice.json)"
check "wrong password is rejected" "$(status -X POST "$GATEWAY/auth-server/v1/auth/login" -H 'Content-Type: application/json' -d '{"username":"alice","password":"nope"}')" "401"
[ -n "$alice" ] && pass "alice got a token" || fail "alice got a token"
check "me says who alice is" "$(curl -s "$GATEWAY/auth-server/v1/auth/me" -H "Authorization: Bearer $alice" | json payload.subject)" "alice"
renewed="$(curl -s -X POST "$GATEWAY/auth-server/v1/auth/refresh" -H 'Content-Type: application/json' -d "{\"refreshToken\":\"$refresh\"}" | json payload.accessToken)"
[ -n "$renewed" ] && pass "a refresh token gives a new token" || fail "a refresh token gives a new token"
check "a refresh token works once" "$(status -X POST "$GATEWAY/auth-server/v1/auth/refresh" -H 'Content-Type: application/json' -d "{\"refreshToken\":\"$refresh\"}")" "401"

echo "2. Order with enough stock: order -> stock -> customer and payment -> shipment -> order"
final="$(place_and_wait "Istanbul, Turkey" LAMP-001 2)"
check "the order ends as Delivered" "$(echo "$final" | json payload.status)" "Delivered"
check "total = 2 x 29.90 (59.8)" "$(echo "$final" | json payload.totalAmount)" "59.8"

echo "3. Order without enough stock"
final="$(place_and_wait "Istanbul, Turkey" CHAIR-001 1)"
check "the order ends as Cancelled" "$(echo "$final" | json payload.status)" "Cancelled"
echo "        note: $(echo "$final" | json payload.note)"

echo "4. The payment is declined"
final="$(place_and_wait "nopay street" LAMP-001 1)"
check "the order ends as Cancelled" "$(echo "$final" | json payload.status)" "Cancelled"
echo "        note: $(echo "$final" | json payload.note)"

echo "5. The parcel cannot be delivered"
final="$(place_and_wait "fail avenue" LAMP-001 1)"
check "the order ends as Cancelled" "$(echo "$final" | json payload.status)" "Cancelled"
echo "        note: $(echo "$final" | json payload.note)"

echo "6. Logout and the history"
check "logout is accepted" "$(status -X POST "$GATEWAY/auth-server/v1/auth/logout" -H 'Content-Type: application/json' -d "{\"refreshToken\":\"$renewed\"}")" "204"
check "the logins are in the history" "$(post auth-server/v1/user-histories/paged-list "$alice" 2>/dev/null | json payload.dataTotalCount 2>/dev/null | awk '{print ($1>0)?"yes":"no"}')" "yes"

echo
if [ "$failures" -eq 0 ]; then echo "ALL CHECKS PASSED"; else echo "$failures CHECK(S) FAILED"; exit 1; fi
