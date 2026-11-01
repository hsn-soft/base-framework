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
# the text normalizer and the video generator: a request is made in the scope of Techsummus, seen by Techsummus and the system tenant and not by Pixel, then deleted (their data is in MongoDB)
login() { curl -s -X POST "$gateway/auth-server/v1/auth/login" -H 'Content-Type: application/json' -d "{\"email\":\"$1\",\"password\":\"$password\"}" | json payload.accessToken; }
count() { curl -s -X POST "$gateway/$2/paged-list" -H "Authorization: Bearer $1" -H 'Content-Type: application/json' -d '{"maxResultCount":100}' | json payload.dataTotalCount; }
techsummus=$(login admin@techsummus.hsx.local); pixel=$(login admin@pixel.hsx.local); system=$(login system-admin@hsx.local)
scope=$(curl -s -X POST "$gateway/content-service/v1/customer-contents/paged-list" -H "Authorization: Bearer $techsummus" -H 'Content-Type: application/json' -d '{}' | json payload.items.0.scopeKey)
crud() { # route, body
  local id
  id=$(curl -s -X POST "$gateway/$1/create" -H "Authorization: Bearer $techsummus" -H 'Content-Type: application/json' -d "$2" | json payload.id)
  check "$1 created" "$([ -n "$id" ] && echo yes)" yes
  check "$1 Techsummus" "$(count "$techsummus" "$1")" 1
  check "$1 Pixel" "$(count "$pixel" "$1")" 0
  check "$1 system" "$(count "$system" "$1")" 1
  check "$1 deleted" "$(curl -s -o /dev/null -w '%{http_code}' -X DELETE "$gateway/$1/$id" -H "Authorization: Bearer $techsummus")" 204
}
crud text-normalizer-service/v1/customer-content-normalized-requests "{\"scopeKey\":\"$scope\",\"customerContentId\":\"$(uuidgen)\",\"domainName\":\"demo.example.com\",\"contentKey\":\"smoke\",\"status\":\"Created\",\"currentMilestone\":\"Start\",\"retryCount\":0}"
crud video-generator-service/v1/video-requests "{\"scopeKey\":\"$scope\",\"refContentId\":\"$(uuidgen)\",\"refContentType\":\"CustomerContent\",\"status\":\"Created\",\"currentMilestone\":\"Start\",\"mediaInputJson\":\"{}\",\"videoProviderKey\":\"creatomate\",\"retryCount\":0}"
[ "$failures" -eq 0 ] && echo "all as expected" || exit 1
