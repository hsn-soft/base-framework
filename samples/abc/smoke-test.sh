#!/usr/bin/env bash
# Runs the three outcomes of an order through the commercial gateway (run samples/abc/run-all.sh first) and checks how each ends:
# any address is delivered, "nopay" is a declined payment and "fail" a parcel the carrier cannot reach (both cancel the order).
# The account is the development one of the README (alice / alice123).
set -uo pipefail
gateway="${1:-http://localhost:5500}"
failures=0

json() { python3 -c "import json,sys; d=json.load(sys.stdin)
for key in sys.argv[1].split('.'):
    d = d[key] if isinstance(d, dict) else d[int(key)]
print('' if d is None else d)" "$1"; }
post() { curl -s -X POST "$gateway/$1" -H "Authorization: Bearer ${token:-}" -H 'Content-Type: application/json' -d "${2:-{\}}"; }

token=$(curl -s -X POST "$gateway/auth-server/v1/auth/login" -H 'Content-Type: application/json' -d '{"username":"alice","password":"alice123"}' | json payload.accessToken)
[ -n "$token" ] || { echo "FAIL login"; exit 1; }
echo "login ok"

product=$(post product-service/v1/products/filter-list '{"sku":"LAMP-001"}')
productId=$(echo "$product" | json payload.0.id)
price=$(echo "$product" | json payload.0.price)

order() { # address, expected status
  local created id status=""
  created=$(post order-service/v1/orders/create "{\"shippingAddress\":\"$1\",\"note\":\"smoke $1\",\"lines\":[{\"productId\":\"$productId\",\"productName\":\"Desk Lamp\",\"unitPrice\":$price,\"quantity\":1}]}")
  id=$(echo "$created" | json payload.id)
  for _ in $(seq 1 60); do
    status=$(post "order-service/v1/orders/get/$id" | json payload.status)
    case $status in Delivered | Cancelled) break ;; esac
    sleep 0.5
  done
  if [ "$status" = "$2" ]; then echo "ok   $1 -> $status"; else echo "FAIL $1 -> '$status', expected $2"; failures=$((failures + 1)); fi
}

order "Kadikoy, Istanbul" Delivered
order "nopay" Cancelled
order "fail" Cancelled
[ "$failures" -eq 0 ] && echo "all outcomes as expected" || exit 1
