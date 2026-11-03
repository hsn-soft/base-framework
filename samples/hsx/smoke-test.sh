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

# a user or a role that was deleted keeps its name taken (soft delete), so what these checks make has a name of its own for every run
run=$(date +%s)

# the sign-in: the password policy, the lockout, the refresh token and the change of a password, each with a user made for it (and deleted after)
as_user() { curl -s -X POST "$gateway/auth-server/v1/auth/login" -H 'Content-Type: application/json' -d "{\"email\":\"$1\",\"password\":\"$2\"}" -o /dev/null -w '%{http_code}'; }
make_user() { # name, password -> id
  curl -s -X POST "$gateway/identity-service/v1/app-users/create" -H "Authorization: Bearer $techsummus" -H 'Content-Type: application/json' -d "{\"userName\":\"$1\",\"email\":\"$1@techsummus.hsx.local\",\"displayName\":\"$1\",\"password\":\"$2\"}" | json payload.id
}
weak=$(curl -s -X POST "$gateway/identity-service/v1/app-users/create" -H "Authorization: Bearer $techsummus" -H 'Content-Type: application/json' -d '{"userName":"weak","email":"weak@techsummus.hsx.local","displayName":"weak","password":"abcdefgh"}' | json errorCode)
check "a password that breaks the policy" "$weak" app_password_policy_violation

good='Passw0rd!x'
locked=$(make_user "smokelock$run" "$good")
for _ in 1 2 3 4 5; do as_user smokelock${run}@techsummus.hsx.local wrong-password > /dev/null; done
check "the right password of a locked-out user" "$(as_user smokelock${run}@techsummus.hsx.local "$good")" 401
curl -s -o /dev/null -X DELETE "$gateway/identity-service/v1/app-users/$locked" -H "Authorization: Bearer $techsummus"

changer=$(make_user "smokechange$run" "$good")
mine=$(curl -s -X POST "$gateway/auth-server/v1/auth/login" -H 'Content-Type: application/json' -d "{\"email\":\"smokechange${run}@techsummus.hsx.local\",\"password\":\"$good\"}" | json payload.accessToken)
change() { curl -s -o /dev/null -w '%{http_code}' -X PUT "$gateway/identity-service/v1/my-account/password" -H "Authorization: Bearer $mine" -H 'Content-Type: application/json' -d "{\"currentPassword\":\"$1\",\"newPassword\":\"$2\"}"; }
check "a change with a wrong current password" "$(change wrong-password 'N3wPassword!')" 400
check "a change of the password" "$(change "$good" 'N3wPassword!')" 204
check "the new password signs in" "$(as_user smokechange${run}@techsummus.hsx.local 'N3wPassword!')" 200
check "the old password does not" "$(as_user smokechange${run}@techsummus.hsx.local "$good")" 401

jar=$(mktemp); first=$(mktemp)
curl -s -c "$jar" -X POST "$gateway/auth-server/v1/auth/login" -H 'Content-Type: application/json' -d "{\"email\":\"smokechange${run}@techsummus.hsx.local\",\"password\":\"N3wPassword!\",\"offlineAccess\":true}" -o /dev/null
cp "$jar" "$first"
refresh() { curl -s -b "$1" -c "$1" -X POST "$gateway/auth-server/v1/auth/token-refresh" -o /dev/null -w '%{http_code}'; }
check "a refresh token gives a new access token" "$(refresh "$jar")" 200
check "the same refresh token a second time" "$(refresh "$first")" 401
check "and the one that took its place ended with it" "$(refresh "$jar")" 401
rm -f "$jar" "$first"
curl -s -o /dev/null -X DELETE "$gateway/identity-service/v1/app-users/$changer" -H "Authorization: Bearer $techsummus"

# the menu and the permissions: the menu of a user holds what its roles let it see; a role with nothing granted sees nothing and is refused by every endpoint; what is granted opens the page and (when the services have read the store again) the endpoint
menu_codes() { curl -s -X POST "$gateway/identity-service/v1/me/menus" -H "Authorization: Bearer $1" -H 'Content-Type: application/json' | python3 -c "import json,sys; d=json.load(sys.stdin)['payload']; print(' '.join(i['code'] for i in d))"; }
check "the menu of the administrator of a reseller" "$(menu_codes "$techsummus" | tr ' ' '\n' | grep -c -E '^(administration|video-platform)$')" 2
check "the menu of the system tenant" "$(menu_codes "$system" | tr ' ' '\n' | grep -c -E '^(administration|video-platform)$')" 2

viewer_role=$(curl -s -X POST "$gateway/identity-service/v1/app-roles/create" -H "Authorization: Bearer $techsummus" -H 'Content-Type: application/json' -d "{\"name\":\"smokeviewer$run\"}" | json payload.id)
viewer=$(make_user "smokeviewer$run" "$good")
curl -s -o /dev/null -X PUT "$gateway/identity-service/v1/app-users/roles" -H "Authorization: Bearer $techsummus" -H 'Content-Type: application/json' -d "{\"id\":\"$viewer\",\"roleIds\":[\"$viewer_role\"]}"
viewer_token=$(curl -s -X POST "$gateway/auth-server/v1/auth/login" -H 'Content-Type: application/json' -d "{\"email\":\"smokeviewer${run}@techsummus.hsx.local\",\"password\":\"$good\"}" | json payload.accessToken)
users_status() { curl -s -o /dev/null -w '%{http_code}' -X POST "$gateway/identity-service/v1/app-users/paged-list" -H "Authorization: Bearer $viewer_token" -H 'Content-Type: application/json' -d '{}'; }
check "a role with nothing granted: the menu" "$(menu_codes "$viewer_token")" ""
check "a role with nothing granted: an endpoint" "$(users_status)" 403
check "a permission that does not exist" "$(curl -s -o /dev/null -w '%{http_code}' -X PUT "$gateway/identity-service/v1/app-roles/$viewer_role/permissions" -H "Authorization: Bearer $techsummus" -H 'Content-Type: application/json' -d '{"codes":["endpoint.nothing.here"]}')" 404
check "granting the page and the read" "$(curl -s -o /dev/null -w '%{http_code}' -X PUT "$gateway/identity-service/v1/app-roles/$viewer_role/permissions" -H "Authorization: Bearer $techsummus" -H 'Content-Type: application/json' -d '{"codes":["page.users.view","endpoint.app-users.read"]}')" 204
check "the granted page is in the menu" "$(menu_codes "$viewer_token" | tr ' ' '\n' | grep -c -x administration)" 1
status=403
for _ in $(seq 1 45); do status=$(users_status); [ "$status" = 200 ] && break; sleep 1; done   # the services keep what they read of the store for Permissions:CacheSeconds
check "the granted endpoint opens (once the store is read again)" "$status" 200
curl -s -o /dev/null -X DELETE "$gateway/identity-service/v1/app-users/$viewer" -H "Authorization: Bearer $techsummus"
curl -s -o /dev/null -X DELETE "$gateway/identity-service/v1/app-roles/$viewer_role" -H "Authorization: Bearer $techsummus"
[ "$failures" -eq 0 ] && echo "all as expected" || exit 1
