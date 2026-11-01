#!/usr/bin/env bash
# Checks what each demo user sees through the commercial gateway (run samples/hsx/run-all.sh first):
#   system-admin@hsx.local     every tenant, every content (the system tenant is under no isolation)
#   admin@techsummus.hsx.local Techsummus and Acme (the tenant under it) and only the content of the video platform it sold
#   admin@pixel.hsx.local      Pixel alone and only the content of the vertical video it sold, though the customer is the same
#   admin@acme.hsx.local       Acme alone, and no content (no subscription)
set -uo pipefail
gateway="${1:-http://localhost:7200}"
password="Hsx-Demo-123"
failures=0

json() { python3 -c "import json,sys; d=json.load(sys.stdin)
for key in sys.argv[1].split('.'):
    d = d[key] if isinstance(d, dict) else d[int(key)]
print('' if d is None else d)" "$1"; }

check() { # description, actual, expected
  if [ "$2" = "$3" ]; then echo "ok   $1 = $2"; else echo "FAIL $1 = '$2', expected '$3'"; failures=$((failures + 1)); fi
}

see() { # email, expected tenants, expected users, expected contents
  local token tenants users contents
  token=$(curl -s -X POST "$gateway/auth-server/v1/auth/login" -H 'Content-Type: application/json' -d "{\"email\":\"$1\",\"password\":\"$password\"}" | json payload.accessToken)
  [ -n "$token" ] || { echo "FAIL login $1"; failures=$((failures + 1)); return; }
  list() { curl -s -X POST "$gateway/$1/paged-list" -H "Authorization: Bearer $token" -H 'Content-Type: application/json' -d '{"maxResultCount":100}' | json payload.dataTotalCount; }
  check "$1 tenants" "$(list identity-service/v1/tenants)" "$2"
  check "$1 users" "$(list identity-service/v1/app-users)" "$3"
  check "$1 contents" "$(list content-service/v1/customer-contents)" "$4"
}

see system-admin@hsx.local 4 4 6
see admin@techsummus.hsx.local 2 2 3
see admin@pixel.hsx.local 1 1 3
see admin@acme.hsx.local 1 1 0
[ "$failures" -eq 0 ] && echo "all as expected" || exit 1
