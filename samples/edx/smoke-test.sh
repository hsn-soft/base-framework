#!/usr/bin/env bash
# Runs what a customer does against the Edx gateway (http://localhost:5600, started by run-all.sh) and checks how each step ends: signing in to a tenant, an archive of XML files, an archive of tables, a model as JSON and as XML,
# the same archive over WCF (SOAP), the files the invoice service learned of through Kafka, and that a tenant sees nothing of another's. gRPC is not here: curl does not speak it.
# The accounts are the sample users of appsettings.development.json of the identity service.
set -euo pipefail
G="${GATEWAY:-http://localhost:5600}"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
failed=0

check() { # name, expected, actual
  if [ "$2" = "$3" ]; then echo "ok    $1"; else echo "FAIL  $1: expected '$2', got '$3'"; failed=1; fi
}
json() { python3 -c "import sys,json; d=json.load(sys.stdin); print($1)"; }
login() { curl -s -X POST "$G/auth-server/v1/auth/login" -H 'Content-Type: application/json' -d "{\"tenant\":\"$1\",\"username\":\"$2\",\"password\":\"$3\"}" | json "d['payload']['accessToken']"; }

veriban="$(login veriban operator veriban-operator-123)"
bilgera="$(login bilgera manager bilgera-manager-123)"
bilgeraAdmin="$(login bilgera admin bilgera-admin-123)"
veribanAdmin="$(login veriban admin veriban-admin-123)"
system="$(login system-tenant admin system-admin-123)"
check "login of a wrong tenant" 401 "$(curl -s -o /dev/null -w '%{http_code}' -X POST "$G/auth-server/v1/auth/login" -H 'Content-Type: application/json' -d '{"tenant":"veriban","username":"manager","password":"bilgera-manager-123"}')"

count() { curl -s -X POST "$G/$2" -H "Authorization: Bearer $1" -H 'Content-Type: application/json' -d '{}' | json "d['payload']['dataTotalCount']"; }
recordedBefore="$(count "$veriban" invoice-service/v1/received-files/paged-list)"
bilgeraFilesBefore="$(count "$bilgera" intake-service/v1/files/paged-list)"

cd "$work"
echo '<invoice><no>1</no></invoice>' > a.xml; echo '<x/>' > b.xml; zip -q xml.zip a.xml b.xml
printf 'a,b\n1,2\n' > t.csv; zip -q tab.zip t.csv
echo '<bad' > bad.xml; zip -q bad.zip bad.xml

upload() { curl -s -o "$work/out.json" -w '%{http_code}' -X POST "$G/intake-service/v1/archives/$2" -H "Authorization: Bearer $1" -F "file=@$3;type=application/zip"; }
check "xml archive" 201 "$(upload "$veriban" xml xml.zip)"
fileId="$(json "d['payload']['id']" < "$work/out.json")"
check "tabular archive" 201 "$(upload "$veriban" tabular tab.zip)"
check "xml archive that is not well formed" 400 "$(upload "$veriban" xml bad.zip)"
check "tables in the xml endpoint" 400 "$(upload "$veriban" xml tab.zip)"

model='{"invoiceNumber":"INV-1","issueDate":"2026-10-27","currency":"TRY","supplierName":"S","customerName":"C","lines":[{"description":"x","quantity":2,"unitPrice":10.5,"vatRate":20}]}'
post() { curl -s -o /dev/null -w '%{http_code}' -X POST "$G/intake-service/v1/models/invoice$2" -H "Authorization: Bearer $1" -H 'Content-Type: application/json' -d "$3"; }
check "model as json" 201 "$(post "$veriban" '' "$model")"
check "model as xml" 201 "$(post "$veriban" '?format=xml' "${model/INV-1/INV-2}")"
check "invalid model" 400 "$(post "$veriban" '' '{"invoiceNumber":"","lines":[]}')"

soap="<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><UploadXmlArchive xmlns=\"urn:edx:intake:v1\"><request><FileName>wcf.zip</FileName><Content>$(base64 < xml.zip | tr -d '\n')</Content></request></UploadXmlArchive></s:Body></s:Envelope>"
wcf() { curl -s -o /dev/null -w '%{http_code}' -X POST "$G/intake-service/wcf/intake.svc" ${1:+-H "Authorization: Bearer $1"} -H 'Content-Type: text/xml; charset=utf-8' -H 'SOAPAction: "urn:edx:intake:v1/IntakeService/UploadXmlArchive"' --data-binary "$soap"; }
check "wcf upload" 200 "$(wcf "$veriban")"
check "wcf upload without a token (a fault)" 500 "$(wcf '')"

sleep 3
check "invoice service learned of the 5 files of veriban" 5 "$(( $(count "$veriban" invoice-service/v1/received-files/paged-list) - recordedBefore ))"
check "bilgera sees none of them" 0 "$(( $(count "$bilgera" intake-service/v1/files/paged-list) - bilgeraFilesBefore ))"
check "bilgera cannot read a file of veriban" 404 "$(curl -s -o /dev/null -w '%{http_code}' -X POST "$G/intake-service/v1/files/get/$fileId" -H "Authorization: Bearer $bilgera")"
check "the system tenant can" 200 "$(curl -s -o /dev/null -w '%{http_code}' -X POST "$G/intake-service/v1/files/get/$fileId" -H "Authorization: Bearer $system")"
check "bilgera sees only its own tenant" 1 "$(curl -s -X POST "$G/identity-service/v1/tenants/filter-list" -H "Authorization: Bearer $bilgeraAdmin" -H 'Content-Type: application/json' -d '{}' | json 'len(d["payload"])')"
check "the system tenant sees them all" 3 "$(curl -s -X POST "$G/identity-service/v1/tenants/filter-list" -H "Authorization: Bearer $system" -H 'Content-Type: application/json' -d '{}' | json 'len(d["payload"])')"

# --- permissions: what a role was granted decides what it may call
status() { curl -s -o /dev/null -w '%{http_code}' "$@"; }
check "the manager of bilgera may not upload (403)" 403 "$(status -X POST "$G/intake-service/v1/archives/xml" -H "Authorization: Bearer $bilgera" -F "file=@$work/xml.zip;type=application/zip")"
check "nor over wcf (a fault)" 500 "$(wcf "$bilgera")"
check "the operator of veriban may not list users (403)" 403 "$(status -X POST "$G/identity-service/v1/users/paged-list" -H "Authorization: Bearer $veriban" -H 'Content-Type: application/json' -d '{}')"
head -c 22000000 /dev/urandom > "$work/big.txt"; (cd "$work" && zip -q big.zip big.txt)
check "the operator is limited to 20 MB (constraint)" 400 "$(upload "$veriban" tabular "$work/big.zip")"
check "the admin of veriban is not (100 MB)" 201 "$(upload "$veribanAdmin" tabular "$work/big.zip")"
myPermissions="$(curl -s -X POST "$G/identity-service/v1/me/permissions/get" -H "Authorization: Bearer $veriban" | json "[p['value'] for p in d['payload'] if p['code']=='constraint.intake.upload.max-bytes'][0]")"
check "the operator reads its own grants, with the value of the limit" 20971520 "$myPermissions"

# the admin of bilgera takes the permission to see emails away from the manager: seen within the few seconds the services keep what they read
rolesOfBilgera="$(curl -s -X POST "$G/identity-service/v1/roles/filter-list" -H "Authorization: Bearer $bilgeraAdmin" -H 'Content-Type: application/json' -d '{}')"
managerRole="$(echo "$rolesOfBilgera" | json "[r['id'] for r in d['payload'] if r['name']=='manager'][0]")"
emails() { curl -s -X POST "$G/identity-service/v1/users/paged-list" -H "Authorization: Bearer $bilgera" -H 'Content-Type: application/json' -d '{}' | json "sorted(set(u['email'] for u in d['payload']['items']))" ; }
check "the manager sees emails" "['admin@bilgera.example', 'manager@bilgera.example']" "$(emails)"
grants="$(curl -s -X POST "$G/identity-service/v1/roles/$managerRole/permissions/get" -H "Authorization: Bearer $bilgeraAdmin" | json "json.dumps({'grants':[{'code':p['code'],'value':p.get('value')} for p in d['payload']]})")"
without="$(echo "$grants" | python3 -c "import sys,json; d=json.load(sys.stdin); d['grants']=[g for g in d['grants'] if g['code']!='operation.identity.user.email.view']; print(json.dumps(d))")"
check "the admin of veriban cannot change a role of bilgera (404)" 404 "$(status -X PUT "$G/identity-service/v1/roles/$managerRole/permissions" -H "Authorization: Bearer $veribanAdmin" -H 'Content-Type: application/json' -d "$without")"
check "the admin of bilgera changes it" 204 "$(status -X PUT "$G/identity-service/v1/roles/$managerRole/permissions" -H "Authorization: Bearer $bilgeraAdmin" -H 'Content-Type: application/json' -d "$without")"
sleep 5
check "the manager's emails are empty now" "['']" "$(emails)"
check "a constraint without a value is refused (400)" 400 "$(status -X PUT "$G/identity-service/v1/roles/$managerRole/permissions" -H "Authorization: Bearer $bilgeraAdmin" -H 'Content-Type: application/json' -d '{"grants":[{"code":"constraint.intake.upload.max-bytes"}]}')"
check "the admin of bilgera gives it back" 204 "$(status -X PUT "$G/identity-service/v1/roles/$managerRole/permissions" -H "Authorization: Bearer $bilgeraAdmin" -H 'Content-Type: application/json' -d "$grants")"
sleep 5
check "the manager sees emails again" "['admin@bilgera.example', 'manager@bilgera.example']" "$(emails)"

exit $failed
