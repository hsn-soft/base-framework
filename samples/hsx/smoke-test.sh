#!/usr/bin/env bash
# Checks what each demo user sees through the commercial gateway (run samples/hsx/run-all.sh first; `smoke-test.sh <gateway> read-only` stops after the counts, so a deployment is not left with test data):
#   system-admin@hsx.local     every tenant, every content (the system tenant is under no isolation)
#   admin@techsummus.hsx.local Techsummus and Acme (the tenant under it) and only the content of the video platform it sold
#   admin@pixel.hsx.local      Pixel alone and only the content of the vertical video it sold, though the customer is the same
#   admin@acme.hsx.local       Acme alone, and no content (no subscription)
set -uo pipefail
gateway="${1:-http://localhost:7200}"
read_only=false; [ "${2:-}" = read-only ] && read_only=true   # read-only: what each demo user sees, and nothing is made (the checks that follow make tenants, users, roles and contents, which stay in the database)
password="Hsx-Demo-123"
failures=0

json() { python3 -c "import json,sys; d=json.load(sys.stdin)
for key in sys.argv[1].split('.'):
    d = d[key] if isinstance(d, dict) else d[int(key)]
print('' if d is None else d)" "$1"; }

check() { # description, actual, expected (an empty expectation is the answer "nothing", never a missing id: the checks that compare ids test them for being there first) ("N+" is at least N: the system tenant sees what the checks of the tenant management leave behind)
  if [ "$2" = "$3" ] || { [[ "$3" == *+ ]] && [[ "$2" =~ ^[0-9]+$ ]] && [ "$2" -ge "${3%+}" ]; }; then echo "ok   $1 = $2"; else echo "FAIL $1 = '$2', expected '$3'"; failures=$((failures + 1)); fi
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

see system-admin@hsx.local 4+ 4+ 6+
see admin@techsummus.hsx.local 2 2 3
see admin@pixel.hsx.local 1 1 3
see admin@acme.hsx.local 1 1 0
if $read_only; then [ "$failures" -eq 0 ] && echo "all as expected (read-only)" || exit 1; exit 0; fi

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

# the tenant management: the system tenant makes a reseller, the reseller makes a company (with the account tenant of its people), a customer and a sale; what it made is its own, and the subscriptions a caller may see are the ones its token names
post() { curl -s -X POST "$gateway/$2" -H "Authorization: Bearer $1" -H 'Content-Type: application/json' -d "$3"; }
code() { curl -s -o /dev/null -w '%{http_code}' -X POST "$gateway/$2" -H "Authorization: Bearer $1" -H 'Content-Type: application/json' -d "$3"; }
reseller_body="{\"title\":\"Smoke Reseller\",\"name\":\"smokereseller$run\",\"adminUser\":{\"userName\":\"admin\",\"email\":\"smokeres$run@hsx.local\",\"displayName\":\"Smoke Reseller Admin\",\"password\":\"$password\"}}"
check "a reseller is not made by a reseller" "$(code "$techsummus" identity-service/v1/tenant-management/resellers/create "$reseller_body")" 403
made=$(post "$system" identity-service/v1/tenant-management/resellers/create "$reseller_body")
check "a reseller is made by the system tenant" "$([ -n "$(echo "$made" | json payload.tenantId)" ] && [ -n "$(echo "$made" | json payload.userId)" ] && echo yes)" yes
reseller_token=$(login "smokeres$run@hsx.local")
check "the first administrator of the reseller signs in" "$([ -n "$reseller_token" ] && echo yes)" yes
company_body="{\"title\":\"Smoke Company\",\"name\":\"smokecompany$run\"}"   # a body is a variable first: bash 3.2 (macOS) splits a quoted body written inside "$( ... )"
company=$(post "$reseller_token" identity-service/v1/tenant-management/companies/create "$company_body")
company_id=$(echo "$company" | json payload.companyId)
check "the reseller makes a company with the tenant of its people" "$([ -n "$company_id" ] && [ -n "$(echo "$company" | json payload.tenantId)" ] && echo yes)" yes
check "a company of a reseller is not made twice" "$(code "$reseller_token" identity-service/v1/tenant-management/companies/create "$company_body")" 400
customer_body="{\"companyId\":\"$company_id\",\"domain\":\"smoke$run.example.com\"}"
customer=$(post "$reseller_token" identity-service/v1/tenant-management/customers/create "$customer_body")
customer_id=$(echo "$customer" | json payload.id)
check "the reseller adds the customer of the company" "$([ -n "$customer_id" ] && echo yes)" yes
check "the same customer asked again is one customer" "$(post "$reseller_token" identity-service/v1/tenant-management/customers/create "$customer_body" | json payload.id)" "$customer_id"
sale_body="{\"customerId\":\"$customer_id\",\"productType\":\"VideoPlatform\"}"
sale=$(post "$reseller_token" identity-service/v1/tenant-management/customer-subscriptions/create "$sale_body")
sale_id=$(echo "$sale" | json payload.id)
check "the reseller sells the product" "$([ -n "$sale_id" ] && echo yes)" yes
check "the same sale asked again is one subscription" "$(post "$reseller_token" identity-service/v1/tenant-management/customer-subscriptions/create "$sale_body" | json payload.id)" "$sale_id"
check "what a reseller made is its own: the companies of the reseller" "$(count "$reseller_token" identity-service/v1/companies)" 1
check "what a reseller made is its own: the companies of another reseller" "$(count "$techsummus" identity-service/v1/companies)" 1
check "what a reseller made is its own: the customers of a third reseller" "$(count "$pixel" identity-service/v1/customers)" 0
check "the subscriptions of the reseller" "$(count "$reseller_token" identity-service/v1/subscriptions)" 1
mine=$(post "$techsummus" identity-service/v1/me/subscriptions/get "{}")
check "my subscriptions: what the token names" "$(echo "$mine" | python3 -c "import json,sys; d=json.load(sys.stdin)['payload']; print(len(d), d[0]['customerDomain'], d[0]['productType'])")" "1 demo.example.com VideoPlatform"
check "my subscriptions: a caller with no scope key" "$(post "$reseller_token" identity-service/v1/me/subscriptions/get "{}" | python3 -c "import json,sys; print(len(json.load(sys.stdin)['payload']))")" 0
[ "$failures" -eq 0 ] && echo "all as expected" || exit 1
