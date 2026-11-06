#!/usr/bin/env bash
# Builds a whole demo of the tenant model through the commercial gateway, as the system operator, and shows what every person sees (run samples/hsx/run-all.sh first, or point it at an environment):
#   samples/hsx/demo-scenario.sh [gateway]     (default http://localhost:7200)
# It makes: a reseller with its administrator, a company with two customers, the video platform sold to both customers by the reseller, three demo contents for each customer, then (as the administrator of the reseller) a Company role
# and user for the company and a Customer role and user for each customer. At the end each of them signs in and the script prints the subscriptions and the contents it sees. Every name carries a run number, so it can be run again.
# The same steps, one request at a time, are in samples/hsx/docs/hsx.postman_collection.json. The password of everybody is Hsx-Demo-123.
set -uo pipefail
gateway="${1:-http://localhost:7200}"
password="Hsx-Demo-123"
tenant_of() { # email -> the tenant it signs in to (the sample's emails say it; the first administrator of a made reseller is the tenant's own name)
  case "$1" in
    system-*@hsx.local) echo system-tenant ;;
    *@tech-summus.hsx.local) echo tech-summus ;;
    *@audio-time.hsx.local) echo audio-time ;;
    *@epartner.hsx.local) echo epartner ;;
    *@reseller.hsx.local) echo "demo-reseller-${run:-0}" ;;
    *@hsx.local) echo "${1%@hsx.local}" ;;
  esac
}
login_body() { printf '{"clientId":"hsx-web","tenant":"%s","account":"%s","password":"%s"%s}' "$(tenant_of "$1")" "$1" "$2" "${3:-}"; }
run=$(date +%s)

json() { python3 -c "import json,sys; d=json.load(sys.stdin)
for key in sys.argv[1].split('.'):
    d = d[key] if isinstance(d, dict) else d[int(key)]
print('' if d is None else d)" "$1"; }
post() { curl -s -X POST "$gateway/$2" -H "Authorization: Bearer $1" -H 'Content-Type: application/json' -d "$3"; }
put() { curl -s -o /dev/null -w '%{http_code}' -X PUT "$gateway/$2" -H "Authorization: Bearer $1" -H 'Content-Type: application/json' -d "$3"; }
login() { curl -s -X POST "$gateway/auth-server/v1/auth/login" -H 'Content-Type: application/json' -d "$(login_body "$1" "$password")" | json payload.accessToken; }
need() { [ -n "$2" ] || { echo "FAILED: $1" >&2; exit 1; }; echo "ok  $1"; }

operator=$(login system-operator@hsx.local); need "the system operator signs in" "$operator"

# the reseller, with its administrator role and its first administrator
reseller_body="{\"title\":\"Demo Reseller $run\",\"name\":\"demo-reseller-$run\",\"adminUser\":{\"userName\":\"admin\",\"email\":\"admin-$run@reseller.hsx.local\",\"displayName\":\"Demo Reseller Admin\"}}"
reseller=$(post "$operator" identity-service/v1/tenant-management/resellers/create "$reseller_body")
tenant_id=$(echo "$reseller" | json payload.tenantId); need "a reseller (tenant $tenant_id)" "$tenant_id"

# the catalogue: a company and two customers
company_id=$(post "$operator" identity-service/v1/companies/create "{\"title\":\"Demo Company $run\",\"name\":\"demo-company-$run\"}" | json payload.id); need "a company" "$company_id"
customer1=$(post "$operator" identity-service/v1/customers/create "{\"companyId\":\"$company_id\",\"domain\":\"first-$run.example.com\"}" | json payload.id); need "the first customer" "$customer1"
customer2=$(post "$operator" identity-service/v1/customers/create "{\"companyId\":\"$company_id\",\"domain\":\"second-$run.example.com\"}" | json payload.id); need "the second customer" "$customer2"

# the sales: the video platform to both customers by the reseller
sale1=$(post "$operator" identity-service/v1/subscriptions/create "{\"tenantId\":\"$tenant_id\",\"customerId\":\"$customer1\",\"productType\":\"VideoPlatform\"}" | json payload.id); need "the video platform sold to the first customer" "$sale1"; scope1="${customer1//-/}:vp"   # the scope key of a sale is the customer and the product (ScopeKeyHelper)
sale2=$(post "$operator" identity-service/v1/subscriptions/create "{\"tenantId\":\"$tenant_id\",\"customerId\":\"$customer2\",\"productType\":\"VideoPlatform\"}" | json payload.id); need "the video platform sold to the second customer" "$sale2"; scope2="${customer2//-/}:vp"   # the scope key of a sale is the customer and the product (ScopeKeyHelper)

# demo contents under each scope key (the system tenant may write under any)
for scope in "$scope1" "$scope2"; do
  for i in 1 2 3; do
    body="{\"scopeKey\":\"$scope\",\"contentKey\":\"demo-page-$i\",\"slugKey\":\"demo-page-$i\",\"normalizeStatus\":\"Completed\",\"videoStatus\":\"Ready\"}"
    [ -n "$(post "$operator" content-service/v1/customer-contents/create "$body" | json payload.id)" ] || { echo "FAILED: content $i of $scope" >&2; exit 1; }
  done
done
echo "ok  three demo contents for each customer"

# the people of the reseller, as its administrator: a Company role bound to the company and a Customer role bound to each customer
admin=$(login "admin-$run@reseller.hsx.local"); need "the administrator of the reseller signs in" "$admin"
make_person() { # name, role body -> signs the user in later as $1-$run@reseller.hsx.local
  local role user
  role=$(post "$admin" identity-service/v1/app-roles/create "$2" | json payload.id); [ -n "$role" ] || { echo "FAILED: role $1" >&2; exit 1; }
  user=$(post "$admin" identity-service/v1/app-users/create "{\"userName\":\"$1\",\"email\":\"$1-$run@reseller.hsx.local\",\"displayName\":\"$1\"}" | json payload.id); [ -n "$user" ] || { echo "FAILED: user $1" >&2; exit 1; }
  roles_body="{\"id\":\"$user\",\"roleIds\":[\"$role\"]}"   # a body is a variable first: bash 3.2 (macOS) splits a quoted body written inside "$( ... )"
  status=$(put "$admin" identity-service/v1/app-users/roles "$roles_body")
  [ "$status" = 200 ] || { echo "FAILED: roles of $1 ($status)" >&2; exit 1; }
  echo "ok  $1 (a user with its own role)"
}
make_person company-person "{\"name\":\"company-role-$run\",\"roleType\":\"Company\",\"companyId\":\"$company_id\"}"
make_person customer1-person "{\"name\":\"customer1-role-$run\",\"roleType\":\"Customer\",\"customerId\":\"$customer1\"}"
make_person customer2-person "{\"name\":\"customer2-role-$run\",\"roleType\":\"Customer\",\"customerId\":\"$customer2\"}"

echo
echo "What each of them sees (a role made just now is known to the services once they read the permission store again, so the script asks until the contents are there):"
for who in "admin-$run" "company-person-$run" "customer1-person-$run" "customer2-person-$run"; do
  token=$(login "$who@reseller.hsx.local")
  subs=$(post "$token" identity-service/v1/me/subscriptions/get "{}" | python3 -c "import json,sys; d=json.load(sys.stdin)['payload']['items']; print(', '.join(sorted(i['customerDomain'] + ' ' + i['productType'] for i in d)))")
  contents=""
  for _ in $(seq 1 60); do
    contents=$(post "$token" content-service/v1/customer-contents/paged-list '{"maxResultCount":100}' 2>/dev/null | json payload.dataTotalCount 2>/dev/null)
    [ -n "$contents" ] && break
    sleep 2
  done
  printf '%-34s subscriptions: %s | contents: %s\n' "$who@reseller.hsx.local" "$subs" "$contents"
done
echo
echo "Expected: the administrator and the company person see both customers (6 contents); each customer person sees only its own (3 contents)."
