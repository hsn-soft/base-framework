#!/usr/bin/env bash
# Runs what the people of the Edx system do against the gateway (http://localhost:5600: identity, auth and invoice services) and the two integrations directly (firm invoice :5510 REST, :5511 gRPC, :5512 WCF; GİB invoice :5540; started by run-all.sh) and checks how each step ends:
#   - the pipeline of an invoice: a zip (CSV, TXT, XLSX, XML) or a model is queued by the firm invoice integration, taken in by the invoice service, becomes a draft, is approved or rejected by a manager, signed and completed; what is refused
#     at the door (hash, zip, type) and what fails later (schema, limit, duplicates); an invoice sent again;
#   - the three ways in (REST, gRPC, WCF) by the three customer examples of samples/edx/client;
#   - what a role may do and see (endpoints, operations, constraints), the menus, and the management of users and roles;
#   - that a tenant sees nothing of another's.
# The accounts are the sample users of appsettings.development.json of the identity service: {tenant}-{role}-123, the roles being admin, manager, operator and user (the system tenant's are system-*).
set -euo pipefail
G="${GATEWAY:-http://localhost:5600}"
# the firm invoice integration (REST :5510, gRPC :5511, WCF :5512) and the GİB invoice integration (REST :5540) are reached directly, the other services through the gateway
F="${FIRM_INVOICE:-http://localhost:5510}"
I="${GIB_INVOICE:-http://localhost:5540}"
FIRM_GRPC="${FIRM_INVOICE_GRPC:-http://localhost:5511}"
FIRM_WCF="${FIRM_INVOICE_WCF:-http://localhost:5512}"
here="$(cd "$(dirname "$0")" && pwd)"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
failed=0

check() { # name, expected, actual
  if [ "$2" = "$3" ]; then echo "ok    $1"; else echo "FAIL  $1: expected '$2', got '$3'"; failed=1; fi
}
json() { python3 -c "import sys,json; d=json.load(sys.stdin); print($1)"; }
login() { curl -s -X POST "$G/auth-server/v1/auth/login" -H 'Content-Type: application/json' -d "{\"tenant\":\"$1\",\"username\":\"$2\",\"password\":\"$3\"}" | json "d['payload']['accessToken']"; }
status() { curl -s -o /dev/null -w '%{http_code}' "$@"; }
call() { curl -s -o "$work/call.json" -w '%{http_code}' "$@"; }
errorCode() { json "d.get('errorCode')" < "$work/call.json"; }

veribanOperator="$(login veriban operator veriban-operator-123)"
veribanManager="$(login veriban manager veriban-manager-123)"
veribanAdmin="$(login veriban admin veriban-admin-123)"
veribanUser="$(login veriban user veriban-user-123)"
bilgeraOperator="$(login bilgera operator bilgera-operator-123)"
bilgeraManager="$(login bilgera manager bilgera-manager-123)"
bilgeraAdmin="$(login bilgera admin bilgera-admin-123)"
system="$(login system-tenant admin system-admin-123)"
check "login of a wrong tenant" 401 "$(status -X POST "$G/auth-server/v1/auth/login" -H 'Content-Type: application/json' -d '{"tenant":"veriban","username":"manager","password":"bilgera-manager-123"}')"
check "all four roles of a tenant can sign in" 4 "$(for u in admin manager operator user; do login veriban "$u" "veriban-$u-123" | head -c 1; done | wc -c | tr -d ' ')"

# the address of a path written as service/v1/...: the two integrations answer on their own host, under the routes of their controllers
urlOf() {
  case "$1" in
    firm-invoice-service/v1/back-office/*) echo "$F/api/$1" ;;
    firm-invoice-service/v1/*) echo "$F/api/firm-invoice-service/v1/commercial/${1#firm-invoice-service/v1/}" ;;
    integration-service/v1/*) echo "$I/api/integration-service/v1/commercial/${1#integration-service/v1/}" ;;
    *) echo "$G/$1" ;;
  esac
}
post() { # token, path, [body]
  local body="${3:-}"
  [ -n "$body" ] || body='{}'
  curl -s -X POST "$(urlOf "$2")" -H "Authorization: Bearer $1" -H 'Content-Type: application/json' -d "$body"
}
sha() { shasum -a 256 "$1" | cut -d' ' -f1; }

# --- the files a customer has: made in a work folder, with an uuid and a number of their own for every run
run="$(date +%s)"
cd "$work"
sample="$here/docs/samples"
# an invoice number is 16 characters (three letters or digits, the year, nine digits): this run's own six digits and the three of the call
prefix="SMK2026$(printf '%06d' $((run % 1000000)))"
number() { echo "$prefix$(printf '%03d' "$1")"; }
uuid() { python3 -c "import uuid; print(uuid.uuid4())"; }
# a file of the samples made into the invoice of the number given: the number is replaced, and (for an XML) the uuid
makeZip() { # file in the samples, number, uuid, output zip, [name in the zip]
  name="${5:-$1}"
  upper="$(echo "$3" | tr '[:lower:]' '[:upper:]')"
  sed -E -e "s/ABC2026[0-9]{9}/$2/g" -e "s/7b0c6a3e-1d2f-4c5a-9e8b-0a1b2c3d4e5f/$3/g" -e "s/7B0C6A3E-1D2F-4C5A-9E8B-0A1B2C3D4E5F/$upper/g" "$sample/$1" > "$work/$name"
  rm -f "$4"
  (cd "$work" && zip -q "$4" "$name")
}
makeXlsxZip() { # number, output: the sheet of the samples with the number replaced
  python3 - "$sample/invoice.xlsx" "$1" "$work/numbered.xlsx" <<'PY'
import sys, zipfile, re
src, number, out = sys.argv[1:4]
with zipfile.ZipFile(src) as a, zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as b:
    for item in a.infolist():
        data = a.read(item.filename)
        if item.filename == "xl/worksheets/sheet1.xml":
            data = re.sub(rb"ABC2026\d{9}", number.encode(), data)
        b.writestr(item, data)
PY
  rm -f "$2"
  (cd "$work" && cp numbered.xlsx invoice.xlsx && zip -q "$2" invoice.xlsx)
}

# one upload: token, route (archives/upload or back-office/upload), zip, uuid, number, source type, [hash]
upload() {
  call -X POST "$(urlOf "firm-invoice-service/v1/${2}")" -H "Authorization: Bearer $1" -F "file=@$3;type=application/zip" -F "invoiceId=$4" -F "invoiceNumber=$5" -F "sourceType=$6" -F "fileHash=${7:-$(sha "$3")}"
}
# the statuses the queue has for a number (sorted, joined by a comma); waits for none of them to be Queued
queueStatuses() { post "$1" firm-invoice-service/v1/queue/paged-list "{\"invoiceNumber\":\"$2\",\"maxResultCount\":50}" | json "','.join(sorted(i['status'] for i in d['payload']['items']))"; }
queueField() { post "$1" firm-invoice-service/v1/queue/paged-list "{\"invoiceNumber\":\"$2\",\"maxResultCount\":50}" | json "[i['$4'] for i in d['payload']['items'] if i['status']=='$3'][0]"; }
waitQueue() { # token, number, expected statuses
  for _ in $(seq 1 30); do
    now="$(queueStatuses "$1" "$2")"
    if [ "$now" = "$3" ]; then echo "$now"; return; fi
    sleep 1
  done
  echo "$now"
}
invoiceOf() { post "$1" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$2\",\"status\":\"${3:-DraftCreated}\"}" | json "d['payload']['items'][0]['id']"; }
invoiceStatuses() { post "$1" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$2\",\"maxResultCount\":50}" | json "','.join(sorted(i['status'] for i in d['payload']['items']))"; }
waitInvoice() { # token, number, expected
  for _ in $(seq 1 30); do
    now="$(invoiceStatuses "$1" "$2")"
    if [ "$now" = "$3" ]; then echo "$now"; return; fi
    sleep 1
  done
  echo "$now"
}
# the envelope state an invoice says of its envelope (a summary kept on the invoice); waits for the one expected
envelopeStateOf() { post "$1" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$2\"}" | json "d['payload']['items'][0]['envelopeState']"; }
waitEnvelope() { # token, number, expected
  for _ in $(seq 1 60); do
    now="$(envelopeStateOf "$1" "$2")"
    if [ "$now" = "$3" ]; then echo "$now"; return; fi
    sleep 1
  done
  echo "$now"
}

# --- the door of the firm invoice integration: what is refused before an answer
n1="$(number 1)"; u1="$(uuid)"
makeZip invoice.csv "$n1" "$u1" "$work/csv.zip"
check "a zip of a csv is queued over REST by an operator" 201 "$(upload "$veribanOperator" archives/upload "$work/csv.zip" "$u1" "$n1" Csv)"
check "  the answer says it is queued, and which host received it" "Queued Edx.Integration.FirmInvoice.Http" "$(json "d['payload']['status']+' '+d['payload']['receivedBy']" < "$work/call.json")"
check "a wrong hash is refused (400)" upload_hash_mismatch "$(upload "$veribanOperator" archives/upload "$work/csv.zip" "$(uuid)" "$(number 2)" Csv "$(printf 'a%.0s' $(seq 1 64))" >/dev/null; errorCode)"
echo 'not a zip' > "$work/plain.zip"
check "a file that is not a zip is refused" upload_not_a_zip "$(upload "$veribanOperator" archives/upload "$work/plain.zip" "$(uuid)" "$(number 3)" Csv >/dev/null; errorCode)"
cp "$sample/invoice.csv" a.csv; cp "$sample/invoice.csv" b.csv; rm -f two.zip; zip -q two.zip a.csv b.csv
check "a zip with two files is refused" upload_not_one_file "$(upload "$veribanOperator" archives/upload "$work/two.zip" "$(uuid)" "$(number 4)" Csv >/dev/null; errorCode)"
check "a csv declared as an xml is refused" upload_type_mismatch "$(upload "$veribanOperator" archives/upload "$work/csv.zip" "$(uuid)" "$(number 5)" Xml >/dev/null; errorCode)"
echo '<bad' > bad.xml; rm -f bad.zip; zip -q bad.zip bad.xml
check "an xml that is not well formed is refused" upload_xml_not_well_formed "$(upload "$veribanOperator" archives/upload "$work/bad.zip" "$(uuid)" "$(number 6)" Xml >/dev/null; errorCode)"
check "an upload without the uuid is refused" validation_error "$(call -X POST "$F/api/firm-invoice-service/v1/commercial/archives/upload" -H "Authorization: Bearer $veribanOperator" -F "file=@$work/csv.zip;type=application/zip" -F "invoiceNumber=$(number 7)" -F "sourceType=Csv" -F "fileHash=$(sha "$work/csv.zip")" >/dev/null; errorCode)"
check "the user role may not upload (403)" 403 "$(upload "$veribanUser" archives/upload "$work/csv.zip" "$(uuid)" "$(number 8)" Csv)"
check "the manager role may not upload (403)" 403 "$(upload "$veribanManager" archives/upload "$work/csv.zip" "$(uuid)" "$(number 8)" Csv)"
head -c 22000000 /dev/urandom > big.txt; rm -f big.zip; zip -q big.zip big.txt
check "the operator is limited to 20 MB (constraint)" 400 "$(upload "$veribanOperator" archives/upload "$work/big.zip" "$(uuid)" "$(number 9)" Txt)"
check "the admin is not (100 MB): the zip is queued" 201 "$(upload "$veribanAdmin" archives/upload "$work/big.zip" "$(uuid)" "$(number 9)" Txt)"

# --- the invoice service takes the invoice in: the draft
check "the invoice of the csv becomes a draft in the queue" DraftCreated "$(waitQueue "$veribanOperator" "$n1" DraftCreated)"
check "  and in the invoice service" DraftCreated "$(waitInvoice "$veribanOperator" "$n1" DraftCreated)"
check "  its total is 2 x 150 + 420.50, with 20 % VAT: 864.60" 864.6 "$(post "$veribanOperator" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$n1\"}" | json "d['payload']['items'][0]['total']")"
check "  the operator does not see the tax ids (no operation tax-id.view)" None "$(post "$veribanOperator" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$n1\"}" | json "d['payload']['items'][0].get('supplierTaxId')")"
check "  the manager does" 1234567801 "$(post "$veribanManager" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$n1\"}" | json "d['payload']['items'][0]['supplierTaxId']")"
check "the same invoice sent again while its draft waits is refused later (the new queue item fails)" "DraftCreated,Failed" "$(upload "$veribanOperator" archives/upload "$work/csv.zip" "$u1" "$n1" Csv >/dev/null; waitQueue "$veribanOperator" "$n1" "DraftCreated,Failed")"
check "  with the reason" True "$(queueField "$veribanOperator" "$n1" Failed statusReason | python3 -c "import sys; print('waits for approval' in sys.stdin.read())")"
check "  and the draft is the only record" DraftCreated "$(invoiceStatuses "$veribanOperator" "$n1")"

n2="$(number 20)"; u2="$(uuid)"
makeZip invoice.txt "$n2" "$u2" "$work/txt.zip"
upload "$veribanOperator" archives/upload "$work/txt.zip" "$u2" "$n2" Txt > /dev/null
check "a txt invoice becomes a draft" DraftCreated "$(waitQueue "$veribanOperator" "$n2" DraftCreated)"
n3="$(number 30)"; u3="$(uuid)"
makeXlsxZip "$n3" "$work/xlsx.zip"
check "a sheet (xlsx) becomes a draft" 201 "$(upload "$veribanOperator" archives/upload "$work/xlsx.zip" "$u3" "$n3" Xls)"
check "  in the queue" DraftCreated "$(waitQueue "$veribanOperator" "$n3" DraftCreated)"
n4="$(number 40)"; u4="$(uuid)"
makeZip invoice.xml "$n4" "$u4" "$work/xml.zip"
check "an xml invoice (checked against the schema) becomes a draft" 201 "$(upload "$veribanOperator" archives/upload "$work/xml.zip" "$u4" "$n4" Xml)"
check "  in the queue" DraftCreated "$(waitQueue "$veribanOperator" "$n4" DraftCreated)"
n5="$(number 50)"; u5="$(uuid)"
check "a model sent as json (the shared api) is queued" 201 "$(call -X POST "$F/api/firm-invoice-service/v1/commercial/models/invoice" -H "Authorization: Bearer $veribanOperator" -H 'Content-Type: application/json' -d "$(sed -E -e "s/ABC2026[0-9]{9}/$n5/" -e "s/7b0c6a3e-1d2f-4c5a-9e8b-0a1b2c3d4e5f/$u5/" "$sample/invoice-model.json")")"
check "  and becomes a draft" DraftCreated "$(waitQueue "$veribanOperator" "$n5" DraftCreated)"
n6="$(number 60)"; u6="$(uuid)"
check "a model sent as xml is queued" 201 "$(call -X POST "$F/api/firm-invoice-service/v1/commercial/models/invoice?format=xml" -H "Authorization: Bearer $veribanOperator" -H 'Content-Type: application/json' -d "$(sed -E -e "s/ABC2026[0-9]{9}/$n6/" -e "s/7b0c6a3e-1d2f-4c5a-9e8b-0a1b2c3d4e5f/$u6/" "$sample/invoice-model.json")")"
check "  and becomes a draft" DraftCreated "$(waitQueue "$veribanOperator" "$n6" DraftCreated)"
check "an invalid model is refused at the door (400)" 400 "$(call -X POST "$F/api/firm-invoice-service/v1/commercial/models/invoice" -H "Authorization: Bearer $veribanOperator" -H 'Content-Type: application/json' -d '{"invoiceNumber":"","lines":[]}')"

# --- what fails in the invoice service, and says why
n7="$(number 70)"; u7="$(uuid)"
makeZip invalid-missing-column.csv "$n7" "$u7" "$work/missing.zip" "$n7.csv"
upload "$veribanOperator" archives/upload "$work/missing.zip" "$u7" "$n7" Csv > /dev/null
check "a csv without a column fails" Failed "$(waitQueue "$veribanOperator" "$n7" Failed)"
check "  at the Validate stage, naming the column" True "$(queueField "$veribanOperator" "$n7" Failed statusReason | python3 -c "import sys; t=sys.stdin.read(); print(t.startswith('Validate') and 'vatRate' in t)")"
check "  the invoice record is Failed too, and visible" Failed "$(invoiceStatuses "$veribanOperator" "$n7")"
n8="$(number 80)"; u8="$(uuid)"
makeZip invalid-bad-number.csv "$n8" "$u8" "$work/badnumber.zip" "$n8.csv"
upload "$veribanOperator" archives/upload "$work/badnumber.zip" "$u8" "$n8" Csv > /dev/null
check "a csv with a number that cannot be read fails" Failed "$(waitQueue "$veribanOperator" "$n8" Failed)"
n9="$(number 90)"; u9="$(uuid)"
makeZip over-limit.csv "$n9" "$u9" "$work/over.zip" "$n9.csv"
upload "$veribanOperator" archives/upload "$work/over.zip" "$u9" "$n9" Csv > /dev/null
check "an invoice over what the operator may make (1 000) fails" Failed "$(waitQueue "$veribanOperator" "$n9" Failed)"
check "  at the Limit stage" True "$(queueField "$veribanOperator" "$n9" Failed statusReason | python3 -c "import sys; print(sys.stdin.read().startswith('Limit'))")"
check "  the admin sends the same invoice and it is a draft (the failed one is replaced)" Completed,DraftCreated "$( { upload "$veribanAdmin" archives/upload "$work/over.zip" "$u9" "$n9" Csv >/dev/null; waitQueue "$veribanAdmin" "$n9" DraftCreated; } | sed 's/^DraftCreated$/Completed,DraftCreated/' )"
check "  the failed record is gone and one draft is left" DraftCreated "$(invoiceStatuses "$veribanAdmin" "$n9")"
check "  the failed queue item was deleted: one item is left" DraftCreated "$(queueStatuses "$veribanAdmin" "$n9")"
n10="$(number 100)"
makeZip invoice.csv "$n10" "$u9" "$work/other.zip"
check "another invoice with the number of a live one fails" Failed "$( { upload "$veribanOperator" archives/upload "$work/other.zip" "$(uuid)" "$n9" Csv >/dev/null; waitQueue "$veribanOperator" "$n9" "DraftCreated,Failed"; } | sed 's/.*Failed.*/Failed/' )"

# --- a person approves or rejects: the permissions of the page, the limit of the role
inv1="$(invoiceOf "$veribanManager" "$n1")"
check "the operator may not approve (no endpoint approve)" 403 "$(call -X POST "$G/invoice-service/v1/sales-invoices/$inv1/approve" -H "Authorization: Bearer $veribanOperator")"
check "the operator may not read the xml (no endpoint xml)" 403 "$(status "$G/invoice-service/v1/sales-invoices/$inv1/xml" -H "Authorization: Bearer $veribanOperator")"
check "the manager reads the draft xml" 200 "$(status "$G/invoice-service/v1/sales-invoices/$inv1/xml" -H "Authorization: Bearer $veribanManager")"
check "  and the xml is the invoice of that number" True "$(curl -s "$G/invoice-service/v1/sales-invoices/$inv1/xml" -H "Authorization: Bearer $veribanManager" | python3 -c "import sys; t=sys.stdin.read(); print('$n1' in t and 'SignatureValue' not in t)")"
check "the manager approves the draft" 204 "$(call -X POST "$G/invoice-service/v1/sales-invoices/$inv1/approve" -H "Authorization: Bearer $veribanManager")"
check "  the invoice is signed (its own life ends there)" Signed "$(waitInvoice "$veribanManager" "$n1" Signed)"
check "  and its envelope is sent to the center and delivered to the post box of the customer" Delivered "$(waitEnvelope "$veribanManager" "$n1" Delivered)"
check "  the invoice says what became of the envelope (code 1300), with its uuid" True "$(post "$veribanManager" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$n1\"}" | json "d['payload']['items'][0]['envelopeStatusCode'] == 1300 and len(d['payload']['items'][0]['envelopeId']) == 36")"
check "  its history starts with the statuses in order and ends with the envelope delivered" True "$(post "$veribanManager" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$n1\"}" | json "[h['value'] for h in d['payload']['items'][0]['history'] if h['kind']=='Status'] == ['Processing','DraftCreated','Approved','Signed'] and d['payload']['items'][0]['history'][-1]['value'] == 'Delivered'")"
check "  and the queue item completed" Completed,Failed "$(waitQueue "$veribanManager" "$n1" "Completed,Failed")"
check "  the signed xml carries the (fake) signature, and the draft is gone" True "$(curl -s "$G/invoice-service/v1/sales-invoices/$inv1/xml" -H "Authorization: Bearer $veribanManager" | python3 -c "import sys; t=sys.stdin.read(); print('SignatureValue' in t and '$n1' in t)")"
check "a delivered invoice cannot be approved again (400)" 400 "$(call -X POST "$G/invoice-service/v1/sales-invoices/$inv1/approve" -H "Authorization: Bearer $veribanManager")"
check "  with its code" invoice_wrong_state "$(errorCode)"
check "a delivered invoice sent again is refused" "Completed,Failed,Failed" "$(upload "$veribanOperator" archives/upload "$work/csv.zip" "$u1" "$n1" Csv >/dev/null; waitQueue "$veribanOperator" "$n1" "Completed,Failed,Failed")"
check "  with the reason" True "$(queueField "$veribanOperator" "$n1" Failed statusReason | python3 -c "import sys; print('signed' in sys.stdin.read())")"

inv2="$(invoiceOf "$veribanManager" "$n2")"
check "the manager rejects a draft with a reason" 204 "$(call -X POST "$G/invoice-service/v1/sales-invoices/$inv2/reject" -H "Authorization: Bearer $veribanManager" -H 'Content-Type: application/json' -d '{"reason":"Customer name is wrong"}')"
check "  a reason is required" 400 "$(status -X POST "$G/invoice-service/v1/sales-invoices/$(invoiceOf "$veribanManager" "$n3")/reject" -H "Authorization: Bearer $veribanManager" -H 'Content-Type: application/json' -d '{"reason":""}')"
check "  the queue item is Rejected, with the reason" Rejected "$(waitQueue "$veribanOperator" "$n2" Rejected)"
check "  the sender reads the reason" "Customer name is wrong" "$(queueField "$veribanOperator" "$n2" Rejected statusReason)"
check "  the invoice record is Rejected and stays visible" Rejected "$(invoiceStatuses "$veribanManager" "$n2")"
check "the rejected invoice is sent again, corrected: a new draft replaces it" "DraftCreated" "$( { upload "$veribanOperator" archives/upload "$work/txt.zip" "$u2" "$n2" Txt >/dev/null; waitQueue "$veribanOperator" "$n2" DraftCreated; } )"
check "  one record is left, the draft" DraftCreated "$(invoiceStatuses "$veribanManager" "$n2")"

n11="$(number 110)"; u11="$(uuid)"
sed -E -e "s/ABC2026[0-9]{9}/$n11/" "$sample/over-limit.csv" > "$work/$n11.csv"; (cd "$work" && rm -f big-amount.zip && zip -q big-amount.zip "$n11.csv")
upload "$veribanAdmin" archives/upload "$work/big-amount.zip" "$u11" "$n11" Csv > /dev/null
check "an invoice of 6 000 000 (made by the admin) becomes a draft" DraftCreated "$(waitQueue "$veribanAdmin" "$n11" DraftCreated)"
check "the manager may not approve over its limit (50 000)" invoice_total_over_limit "$(call -X POST "$G/invoice-service/v1/sales-invoices/$(invoiceOf "$veribanAdmin" "$n11")/approve" -H "Authorization: Bearer $veribanManager" >/dev/null; errorCode)"
check "  while the admin may" 204 "$(call -X POST "$G/invoice-service/v1/sales-invoices/$(invoiceOf "$veribanAdmin" "$n11")/approve" -H "Authorization: Bearer $veribanAdmin")"
check "  and it is signed" Signed "$(waitInvoice "$veribanAdmin" "$n11" Signed)"
check "the operator does not see an invoice above its view limit (1 000): the list" 0 "$(post "$veribanOperator" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$n11\"}" | json "d['payload']['dataTotalCount']")"
check "  nor the invoice itself (404)" 404 "$(call -X POST "$G/invoice-service/v1/sales-invoices/get/$(invoiceOf "$veribanAdmin" "$n11" Signed)" -H "Authorization: Bearer $veribanOperator")"

# --- the back office has its own routes: the channel is told by the route
n12="$(number 120)"; u12="$(uuid)"
makeZip invoice.csv "$n12" "$u12" "$work/office.zip"
check "the upload page queues a zip" 201 "$(upload "$veribanOperator" back-office/upload "$work/office.zip" "$u12" "$n12" Csv)"
check "  with the channel of the page" BackOfficeUploadPage "$(json "d['payload']['channelType']" < "$work/call.json")"
n13="$(number 130)"; u13="$(uuid)"
check "the create page queues a model" 201 "$(call -X POST "$F/api/firm-invoice-service/v1/back-office/create" -H "Authorization: Bearer $veribanOperator" -H 'Content-Type: application/json' -d "$(sed -E -e "s/ABC2026[0-9]{9}/$n13/" -e "s/7b0c6a3e-1d2f-4c5a-9e8b-0a1b2c3d4e5f/$u13/" "$sample/invoice-model.json")")"
check "  with the channel of the page" BackOfficeCreatePage "$(json "d['payload']['channelType']" < "$work/call.json")"
check "the user role may not use the pages (403)" 403 "$(call -X POST "$F/api/firm-invoice-service/v1/back-office/create" -H "Authorization: Bearer $veribanUser" -H 'Content-Type: application/json' -d '{}')"
check "the file of a queue item can be downloaded (queue download)" 200 "$(status "$F/api/firm-invoice-service/v1/commercial/queue/$(queueField "$veribanOperator" "$n12" DraftCreated id 2>/dev/null || queueField "$veribanOperator" "$n12" Queued id)/download" -H "Authorization: Bearer $veribanOperator")"
check "the user role reads the queue but may not download (403)" 403 "$(status "$F/api/firm-invoice-service/v1/commercial/queue/$(queueField "$veribanOperator" "$n12" DraftCreated id 2>/dev/null || queueField "$veribanOperator" "$n12" Queued id)/download" -H "Authorization: Bearer $veribanUser")"

# --- the e-invoice: the center (the mock) is the post box of the customer's integrator; what was delivered is the purchase invoice of the customer
check "bilgera (the customer) has the invoice among the ones that came in" 1 "$(post "$bilgeraManager" invoice-service/v1/purchase-invoices/paged-list "{\"number\":\"$n1\"}" | json "d['payload']['dataTotalCount']")"
check "  from the supplier, Veriban" 1234567801 "$(post "$bilgeraManager" invoice-service/v1/purchase-invoices/paged-list "{\"number\":\"$n1\"}" | json "d['payload']['items'][0]['supplierRegisterNumber']")"
purchaseId="$(post "$bilgeraManager" invoice-service/v1/purchase-invoices/paged-list "{\"number\":\"$n1\"}" | json "d['payload']['items'][0]['id']")"
check "  the manager reads its xml, the signed UBL-TR invoice" True "$(curl -s "$G/invoice-service/v1/purchase-invoices/$purchaseId/xml" -H "Authorization: Bearer $bilgeraManager" | python3 -c "import sys; t=sys.stdin.read(); print('$n1' in t and 'SignatureValue' in t and 'TEMELFATURA' in t)")"
check "  the operator of bilgera may not read the xml (403)" 403 "$(status "$G/invoice-service/v1/purchase-invoices/$purchaseId/xml" -H "Authorization: Bearer $bilgeraOperator")"
check "  its envelope reaches 1300 at the center (the center asked our post box, and we asked the center)" 1300 "$(for _ in $(seq 1 60); do code="$(post "$bilgeraManager" invoice-service/v1/purchase-invoices/paged-list "{\"number\":\"$n1\"}" | json "d['payload']['items'][0]['envelopeStatusCode']")"; [ "$code" = 1300 ] && break; sleep 1; done; echo "$code")"
envelopeUuid="$(post "$veribanManager" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$n1\"}" | json "d['payload']['items'][0]['envelopeId']")"
check "the envelope is asked of the integration by its uuid: its history has the center's 1300" True "$(post "$veribanManager" "integration-service/v1/envelopes/outbound/by-envelope/$envelopeUuid" | json "any(e['code'] == 1300 for e in d['payload']['history'])")"
check "  the receiver's side asks its own copy by the same uuid, and sees what the center says" 1300 "$(post "$bilgeraManager" "integration-service/v1/envelopes/inbound/by-envelope/$envelopeUuid" | json "d['payload']['centerStatusCode']")"
check "  an envelope of another tenant is not found (404)" 404 "$(call -X POST "$(urlOf "integration-service/v1/envelopes/outbound/by-envelope/$envelopeUuid")" -H "Authorization: Bearer $bilgeraManager")"
check "veriban (the supplier) does not have it among the ones that came in" 0 "$(post "$veribanManager" invoice-service/v1/purchase-invoices/paged-list "{\"number\":\"$n1\"}" | json "d['payload']['dataTotalCount']")"
check "the envelope of the invoice is delivered, on the outbound list of veriban" Delivered "$(post "$veribanManager" integration-service/v1/envelopes/outbound/paged-list "{\"invoiceNumber\":\"$n1\"}" | json "d['payload']['items'][0]['state']")"
check "  with the status code of the center for a delivered envelope (1300)" 1300 "$(post "$veribanManager" integration-service/v1/envelopes/outbound/paged-list "{\"invoiceNumber\":\"$n1\"}" | json "d['payload']['items'][0]['statusCode']")"
check "  and on the inbound list of bilgera, accepted" Accepted "$(post "$bilgeraManager" integration-service/v1/envelopes/inbound/paged-list "{\"invoiceNumber\":\"$n1\"}" | json "d['payload']['items'][0]['state']")"
check "the user role may not read the envelopes (403)" 403 "$(status -X POST "$I/api/integration-service/v1/commercial/envelopes/outbound/paged-list" -H "Authorization: Bearer $veribanUser" -H 'Content-Type: application/json' -d '{}')"

n14="$(number 140)"; u14="$(uuid)"
check "an invoice to a customer the center does not know is queued" 201 "$(call -X POST "$F/api/firm-invoice-service/v1/commercial/models/invoice" -H "Authorization: Bearer $veribanOperator" -H 'Content-Type: application/json' -d "$(sed -E -e "s/ABC2026[0-9]{9}/$n14/" -e "s/7b0c6a3e-1d2f-4c5a-9e8b-0a1b2c3d4e5f/$u14/" -e 's/1234567802/1234567899/' "$sample/invoice-model.json")")"
check "  and becomes a draft" DraftCreated "$(waitQueue "$veribanOperator" "$n14" DraftCreated)"
check "  the manager approves it" 204 "$(call -X POST "$G/invoice-service/v1/sales-invoices/$(invoiceOf "$veribanManager" "$n14")/approve" -H "Authorization: Bearer $veribanManager")"
check "  the invoice is signed, and its envelope fails (the receiver is not an e-invoice user)" Failed "$(waitEnvelope "$veribanManager" "$n14" Failed)"
check "  the invoice stays signed" Signed "$(invoiceStatuses "$veribanManager" "$n14")"
check "  and the queue item says the delivery failed" Failed "$(waitQueue "$veribanManager" "$n14" Failed)"

# --- the commercial invoice (TICARIFATURA): the receiver answers it with an application response, which goes back through the center to the supplier
answers() { post "$1" invoice-service/v1/purchase-invoices/paged-list "{\"number\":\"$2\"}" | json "','.join(r['kind'] for r in d['payload']['items'][0]['responses'])"; }
nC="$(number 150)"; uC="$(uuid)"
makeZip invoice-commercial.csv "$nC" "$uC" "$work/commercial.zip"
check "a commercial invoice (the column invoiceProfile says TICARIFATURA) is queued" 201 "$(upload "$veribanOperator" archives/upload "$work/commercial.zip" "$uC" "$nC" Csv)"
check "  and becomes a draft" DraftCreated "$(waitQueue "$veribanOperator" "$nC" DraftCreated)"
check "  the draft says it is commercial" TICARIFATURA "$(post "$veribanManager" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$nC\"}" | json "d['payload']['items'][0]['profile']")"
check "  the manager approves it" 204 "$(call -X POST "$G/invoice-service/v1/sales-invoices/$(invoiceOf "$veribanManager" "$nC")/approve" -H "Authorization: Bearer $veribanManager")"
check "  it is signed, and its envelope is delivered to bilgera" Delivered "$(waitEnvelope "$veribanManager" "$nC" Delivered)"
check "  nobody has answered it yet: the supplier waits" "" "$(post "$veribanManager" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$nC\"}" | json "','.join(r['kind'] for r in d['payload']['items'][0]['responses'])")"
purchaseC=""
for _ in $(seq 1 30); do purchaseC="$(post "$bilgeraManager" invoice-service/v1/purchase-invoices/paged-list "{\"number\":\"$nC\"}" | json "d['payload']['items'][0]['id'] if d['payload']['items'] else ''")"; [ -n "$purchaseC" ] && break; sleep 1; done
check "bilgera has it among the ones that came in, a commercial one" TICARIFATURA "$(post "$bilgeraManager" invoice-service/v1/purchase-invoices/paged-list "{\"number\":\"$nC\"}" | json "d['payload']['items'][0]['profile']")"
respond() { call -X POST "$G/invoice-service/v1/purchase-invoices/$2/respond" -H "Authorization: Bearer $1" -H 'Content-Type: application/json' -d "$3"; }
check "the operator of bilgera may not answer (403)" 403 "$(respond "$bilgeraOperator" "$purchaseC" '{"accept":true}')"
check "a basic invoice is not answered (400)" purchase_invoice_not_commercial "$(respond "$bilgeraManager" "$purchaseId" '{"accept":true}' >/dev/null; errorCode)"
check "a refusal needs a reason (400)" purchase_invoice_refusal_needs_reason "$(respond "$bilgeraManager" "$purchaseC" '{"accept":false}' >/dev/null; errorCode)"
check "the manager of bilgera accepts the invoice" 200 "$(respond "$bilgeraManager" "$purchaseC" '{"accept":true,"note":"Tamam"}')"
check "  it is not answered twice (400)" purchase_invoice_already_answered "$(respond "$bilgeraManager" "$purchaseC" '{"accept":true}' >/dev/null; errorCode)"
check "  the answer is signed and kept with the invoice" Accepted "$(for _ in $(seq 1 40); do a="$(answers "$bilgeraManager" "$nC")"; [ -n "$a" ] && break; sleep 1; done; echo "$a")"
check "  its envelope is delivered to the supplier (the center says 1300)" 1300 "$(for _ in $(seq 1 90); do c="$(post "$bilgeraManager" invoice-service/v1/purchase-invoices/paged-list "{\"number\":\"$nC\"}" | json "d['payload']['items'][0]['responses'][0].get('envelopeStatusCode') if d['payload']['items'][0]['responses'] else ''")"; [ "$c" = 1300 ] && break; sleep 1; done; echo "$c")"
check "the supplier (veriban) has the answer with its sales invoice: accepted" Accepted "$(for _ in $(seq 1 40); do a="$(post "$veribanManager" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$nC\"}" | json "','.join(r['kind'] for r in d['payload']['items'][0]['responses'])")"; [ -n "$a" ] && break; sleep 1; done; echo "$a")"
check "  with what the receiver said" Tamam "$(post "$veribanManager" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$nC\"}" | json "d['payload']['items'][0]['responses'][0].get('note') or ''")"

nD="$(number 160)"; uD="$(uuid)"
makeZip invoice-commercial.csv "$nD" "$uD" "$work/commercial2.zip"
upload "$veribanOperator" archives/upload "$work/commercial2.zip" "$uD" "$nD" Csv > /dev/null
waitQueue "$veribanOperator" "$nD" DraftCreated > /dev/null
call -X POST "$G/invoice-service/v1/sales-invoices/$(invoiceOf "$veribanManager" "$nD")/approve" -H "Authorization: Bearer $veribanManager" > /dev/null
purchaseD=""
for _ in $(seq 1 60); do purchaseD="$(post "$bilgeraManager" invoice-service/v1/purchase-invoices/paged-list "{\"number\":\"$nD\"}" | json "d['payload']['items'][0]['id'] if d['payload']['items'] else ''")"; [ -n "$purchaseD" ] && break; sleep 1; done
check "a second commercial invoice is refused by bilgera, with a reason" 200 "$(respond "$bilgeraManager" "$purchaseD" '{"accept":false,"note":"Siparis edilmedi"}')"
check "  the supplier hears the refusal and why" "Rejected Siparis edilmedi" "$(for _ in $(seq 1 90); do a="$(post "$veribanManager" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$nD\"}" | json "(d['payload']['items'][0]['responses'][0]['kind'] + ' ' + (d['payload']['items'][0]['responses'][0].get('note') or '')) if d['payload']['items'][0]['responses'] else ''")"; [ -n "$a" ] && break; sleep 1; done; echo "$a")"

# --- the signer service signs what a person approved (the invoices above were signed by it); it is reached through the gateway like the other services
check "the signer service answers its health through the gateway" 200 "$(status "$G/signer-service/health")"

# --- the two integrations are not behind the gateway: they are called directly, and a browser (the back office) may call them from its own origin
check "the gateway does not route the firm invoice integration (404)" 404 "$(status -X POST "$G/firm-invoice-service/v1/queue/paged-list" -H "Authorization: Bearer $veribanOperator" -H 'Content-Type: application/json' -d '{}')"
check "the gateway does not route the GİB invoice integration (404)" 404 "$(status -X POST "$G/integration-service/v1/envelopes/outbound/paged-list" -H "Authorization: Bearer $veribanManager" -H 'Content-Type: application/json' -d '{}')"
check "the firm invoice integration answers the preflight of the back office" http://localhost:4200 "$(curl -s -D - -o /dev/null -X OPTIONS "$F/api/firm-invoice-service/v1/commercial/queue/paged-list" -H 'Origin: http://localhost:4200' -H 'Access-Control-Request-Method: POST' -H 'Access-Control-Request-Headers: authorization,content-type' | tr -d '\r' | awk -F': ' 'tolower($1)=="access-control-allow-origin"{print $2}')"
check "  and does not let another origin in" "" "$(curl -s -D - -o /dev/null -X OPTIONS "$I/api/integration-service/v1/commercial/envelopes/outbound/paged-list" -H 'Origin: http://evil.example' -H 'Access-Control-Request-Method: POST' | tr -d '\r' | awk -F': ' 'tolower($1)=="access-control-allow-origin"{print $2}')"
check "the GİB invoice integration answers the preflight of the back office" http://localhost:4200 "$(curl -s -D - -o /dev/null -X OPTIONS "$I/api/integration-service/v1/commercial/envelopes/outbound/paged-list" -H 'Origin: http://localhost:4200' -H 'Access-Control-Request-Method: POST' -H 'Access-Control-Request-Headers: authorization,content-type' | tr -d '\r' | awk -F': ' 'tolower($1)=="access-control-allow-origin"{print $2}')"

# --- the three ways in, by the customer examples (each is a small program: REST, gRPC and WCF)
export EDX_GATEWAY="$G" EDX_FIRM_INVOICE_REST="$F" EDX_FIRM_INVOICE_GRPC="$FIRM_GRPC" EDX_FIRM_INVOICE_WCF="$FIRM_WCF" EDX_TENANT=veriban EDX_USER=operator EDX_PASSWORD=veriban-operator-123
wayIndex=900
for way in Rest Grpc Wcf; do
  wayIndex=$((wayIndex + 1))
  numberWay="$(number "$wayIndex")"
  makeZip invoice.csv "$numberWay" "$u1" "$work/way.zip" >/dev/null
  sed -E -e "s/ABC2026[0-9]{9}/$numberWay/" "$sample/invoice.csv" > "$work/$numberWay.csv"
  out="$(dotnet run --project "$here/client/Edx.Client.$way" --verbosity quiet -- "$work/$numberWay.csv" "$numberWay" 2>&1 || true)"
  check "the $way client queues an invoice" True "$(echo "$out" | python3 -c "import sys; t=sys.stdin.read(); print('Queued' in t or 'queued' in t)")"
  check "  it becomes a draft" DraftCreated "$(waitQueue "$veribanOperator" "$numberWay" DraftCreated)"
  expected="Edx.Integration.FirmInvoice.$way"; [ "$way" = Rest ] && expected="Edx.Integration.FirmInvoice.Http"
  check "  and the queue says which host received it" "$expected" "$(queueField "$veribanOperator" "$numberWay" DraftCreated receivedBy)"
done

# --- tenants: what a tenant does not see
check "bilgera sees none of the queue of veriban" 0 "$(post "$bilgeraOperator" firm-invoice-service/v1/queue/paged-list "{\"invoiceNumber\":\"$prefix\"}" | json "d['payload']['dataTotalCount']")"
check "bilgera sees none of the invoices of veriban" 0 "$(post "$bilgeraManager" invoice-service/v1/sales-invoices/paged-list "{\"number\":\"$prefix\"}" | json "d['payload']['dataTotalCount']")"
check "bilgera cannot read an invoice of veriban (404)" 404 "$(call -X POST "$G/invoice-service/v1/sales-invoices/get/$inv1" -H "Authorization: Bearer $bilgeraManager")"
check "the system tenant can" 200 "$(call -X POST "$G/invoice-service/v1/sales-invoices/get/$inv1" -H "Authorization: Bearer $system")"
check "bilgera sees only its own tenant" 1 "$(post "$bilgeraAdmin" identity-service/v1/tenants/filter-list | json 'len(d["payload"])')"
check "the system tenant sees them all" 3 "$(post "$system" identity-service/v1/tenants/filter-list | json 'len(d["payload"])')"

# --- permissions: what a role was granted decides what it may call
roleOf() { curl -s -X POST "$G/identity-service/v1/roles/filter-list" -H "Authorization: Bearer $1" -H 'Content-Type: application/json' -d '{}' | json "[r['id'] for r in d['payload'] if r['name']=='$2'][0]"; }
grantsOf() { curl -s -X POST "$G/identity-service/v1/roles/$2/permissions/get" -H "Authorization: Bearer $1" | json "json.dumps({'grants':[{'code':p['code'],'value':p.get('value')} for p in d['payload']]})"; }
without() { python3 -c "import sys,json; d=json.load(sys.stdin); d['grants']=[g for g in d['grants'] if g['code']!='$1']; print(json.dumps(d))"; }
setGrants() { call -X PUT "$G/identity-service/v1/roles/$2/permissions" -H "Authorization: Bearer $1" -H 'Content-Type: application/json' -d "$3"; }

check "the operator of veriban may not list users (403)" 403 "$(status -X POST "$G/identity-service/v1/users/paged-list" -H "Authorization: Bearer $veribanOperator" -H 'Content-Type: application/json' -d '{}')"
myLimit="$(curl -s -X POST "$G/identity-service/v1/me/permissions/get" -H "Authorization: Bearer $veribanOperator" | json "[p['value'] for p in d['payload'] if p['code']=='constraint.firm-invoice.upload.max-bytes'][0]")"
check "the operator reads its own grants, with the value of the limit" 20971520 "$myLimit"

# --- the menu of a user: only what it may see, each page with what it needs
menuOf() { curl -s -X POST "$G/identity-service/v1/me/menus/get" -H "Authorization: Bearer $1" | json "$2"; }
titles='[m["title"]+(":"+",".join(c["title"] for c in m["children"]) if m["children"] else "") for m in d["payload"]]'
check "the menu of the operator: the entry pages, the invoices (and the ones that came in) and the reports" "['İş', 'Fatura girişi:Fatura oluştur,Dosya yükle,Kuyruk', 'Fatura yönetimi:Satış faturaları,Alış faturaları,Fatura raporları']" "$(menuOf "$veribanOperator" "$titles")"
check "the menu of the manager: the queue, the drafts, the envelopes and the users" "['İş', 'Fatura girişi:Kuyruk', 'Fatura yönetimi:Satış faturaları,Alış faturaları,Taslak onayı,Fatura raporları', 'Entegrasyon:Zarflar', 'Yönetim', 'Kimlik yönetimi:Kullanıcılar']" "$(menuOf "$bilgeraManager" "$titles")"
check "the menu of the user only reads" "['İş', 'Fatura girişi:Kuyruk', 'Fatura yönetimi:Satış faturaları,Alış faturaları,Fatura raporları']" "$(menuOf "$veribanUser" "$titles")"
check "the menu of the admin has all of it" "['İş', 'Fatura girişi:Fatura oluştur,Dosya yükle,Kuyruk', 'Fatura yönetimi:Satış faturaları,Alış faturaları,Taslak onayı,Fatura raporları', 'Entegrasyon:Zarflar', 'Yönetim', 'Kimlik yönetimi:Kullanıcılar,Roller']" "$(menuOf "$bilgeraAdmin" "$titles")"
check "a page says what its buttons need and whether the manager has them (approve: yes)" True "$(menuOf "$veribanManager" "[p['granted'] for m in d['payload'] if m['children'] for c in m['children'] if c['code'].endswith('.drafts') for p in c['permissions'] if p['code']=='endpoint.invoice.approve'][0]")"

# --- identity management: roles and users of a tenant, made and given permissions through the endpoints
check "the admin of veriban makes a role" 201 "$(call -X POST "$G/identity-service/v1/roles/create" -H "Authorization: Bearer $veribanAdmin" -H 'Content-Type: application/json' -d "{\"name\":\"auditor$run\"}")"
auditorRole="$(json "d['payload']['id']" < "$work/call.json")"
check "  the same name twice is refused" role_name_duplicate "$(call -X POST "$G/identity-service/v1/roles/create" -H "Authorization: Bearer $veribanAdmin" -H 'Content-Type: application/json' -d "{\"name\":\"auditor$run\"}" >/dev/null; errorCode)"
newUser() { echo "{\"userName\":\"auditor$run\",\"email\":\"a@veriban.example\",\"displayName\":\"Auditor\",\"password\":\"auditor-123\",\"roleIds\":[$1]}"; }
check "  and a user with that role" 201 "$(call -X POST "$G/identity-service/v1/users/create" -H "Authorization: Bearer $veribanAdmin" -H 'Content-Type: application/json' -d "$(newUser "\"$auditorRole\"")")"
auditorUser="$(json "d['payload']['id']" < "$work/call.json")"
auditor="$(login veriban "auditor$run" auditor-123)"
check "a user with no permission has no menu" "[]" "$(menuOf "$auditor" 'd["payload"]')"
check "  and is refused by every endpoint (403)" 403 "$(status -X POST "$G/invoice-service/v1/sales-invoices/paged-list" -H "Authorization: Bearer $auditor" -H 'Content-Type: application/json' -d '{}')"
check "the admin gives the role the page, the read and a limit" 204 "$(setGrants "$veribanAdmin" "$auditorRole" '{"grants":[{"code":"page.invoice.view"},{"code":"endpoint.invoice.read"},{"code":"constraint.invoice.max-view-amount","value":"500"}]}')"
check "a page permission carries no value (a value is refused)" 400 "$(setGrants "$veribanAdmin" "$auditorRole" '{"grants":[{"code":"page.invoice.view","value":"1"}]}')"
sleep 5
check "the menu is the invoices now" "['İş', 'Fatura yönetimi:Satış faturaları']" "$(menuOf "$auditor" "$titles")"
check "the auditor reads the invoices up to its limit of 500" 200 "$(status -X POST "$G/invoice-service/v1/sales-invoices/paged-list" -H "Authorization: Bearer $auditor" -H 'Content-Type: application/json' -d '{}')"
check "the role cannot be deleted while a user has it" role_in_use "$(call -X DELETE "$G/identity-service/v1/roles/$auditorRole" -H "Authorization: Bearer $veribanAdmin" >/dev/null; errorCode)"
check "a user name taken in the tenant is refused" user_name_duplicate "$(call -X POST "$G/identity-service/v1/users/create" -H "Authorization: Bearer $veribanAdmin" -H 'Content-Type: application/json' -d "$(newUser "")" >/dev/null; errorCode)"
foreignRole="$(roleOf "$bilgeraAdmin" manager)"
updateUser() { echo "{\"id\":\"$auditorUser\",\"email\":\"a@veriban.example\",\"displayName\":\"Auditor\",\"isActive\":true,\"roleIds\":[\"$1\"]}"; }
check "a role of another tenant cannot be given (404)" 404 "$(status -X PUT "$G/identity-service/v1/users" -H "Authorization: Bearer $veribanAdmin" -H 'Content-Type: application/json' -d "$(updateUser "$foreignRole")")"
check "an admin does not delete itself" user_deletes_itself "$(call -X DELETE "$G/identity-service/v1/users/$(curl -s -X POST "$G/identity-service/v1/users/paged-list" -H "Authorization: Bearer $veribanAdmin" -H 'Content-Type: application/json' -d '{"userName":"admin"}' | json "d['payload']['items'][0]['id']")" -H "Authorization: Bearer $veribanAdmin" >/dev/null; errorCode)"
check "the user is deleted" 204 "$(status -X DELETE "$G/identity-service/v1/users/$auditorUser" -H "Authorization: Bearer $veribanAdmin")"
check "and then the role" 204 "$(status -X DELETE "$G/identity-service/v1/roles/$auditorRole" -H "Authorization: Bearer $veribanAdmin")"

# --- the admin of bilgera takes a permission away from the manager, and a limit: seen within the few seconds the services keep what they read
managerRole="$(roleOf "$bilgeraAdmin" manager)"
emails() { curl -s -X POST "$G/identity-service/v1/users/paged-list" -H "Authorization: Bearer $bilgeraManager" -H 'Content-Type: application/json' -d '{}' | json "len(set(u['email'] for u in d['payload']['items'] if u['email']))"; }
grants="$(grantsOf "$bilgeraAdmin" "$managerRole")"
check "the manager sees the emails of the four users (operation users.email.view)" 4 "$(emails)"
check "the admin of veriban cannot change a role of bilgera (404)" 404 "$(setGrants "$veribanAdmin" "$managerRole" "$(echo "$grants" | without operation.users.email.view)")"
check "the admin of bilgera takes the operation away" 204 "$(setGrants "$bilgeraAdmin" "$managerRole" "$(echo "$grants" | without operation.users.email.view)")"
sleep 5
check "the manager's emails are empty now" 0 "$(emails)"
check "a constraint without a value is refused (400)" 400 "$(setGrants "$bilgeraAdmin" "$managerRole" '{"grants":[{"code":"constraint.firm-invoice.upload.max-bytes"}]}')"
check "the admin of bilgera takes the approve endpoint away" 204 "$(setGrants "$bilgeraAdmin" "$managerRole" "$(echo "$grants" | without endpoint.invoice.approve)")"
sleep 5
check "the manager may not approve now (403)" 403 "$(status -X POST "$G/invoice-service/v1/sales-invoices/$inv1/approve" -H "Authorization: Bearer $bilgeraManager")"
check "the admin of bilgera takes the view limit away (a constraint)" 204 "$(setGrants "$bilgeraAdmin" "$managerRole" "$(echo "$grants" | without constraint.invoice.max-view-amount)")"
sleep 5
check "the manager is refused (403) before anything is read" 403 "$(call -X POST "$G/invoice-service/v1/sales-invoices/paged-list" -H "Authorization: Bearer $bilgeraManager" -H 'Content-Type: application/json' -d '{}')"
check "  and the answer says which constraint it lacks" True "$(json "'constraint.invoice.max-view-amount' in d['statusMessages'][0]" < "$work/call.json")"
check "the admin of bilgera gives it all back" 204 "$(setGrants "$bilgeraAdmin" "$managerRole" "$grants")"
sleep 5
check "the manager sees the emails again" 4 "$(emails)"

# --- invoice reports: a page of its own, an action, an operation and a limit in days
check "the admin makes a report of a week (action generate)" 201 "$(call -X POST "$G/invoice-service/v1/invoice-reports/generate" -H "Authorization: Bearer $veribanAdmin" -H 'Content-Type: application/json' -d '{"periodStart":"2026-10-20","periodEnd":"2026-10-28","currency":"TRY"}')"
reportId="$(json "d['payload']['id']" < "$work/call.json")"
check "  and sees what it amounts to (operation total.view)" True "$(json "d['payload']['totalAmount'] is not None and d['payload']['invoiceCount'] > 0" < "$work/call.json")"
check "a period longer than the days it may look back is refused (constraint max-days 3 650)" 400 "$(call -X POST "$G/invoice-service/v1/invoice-reports/generate" -H "Authorization: Bearer $veribanAdmin" -H 'Content-Type: application/json' -d '{"periodStart":"2010-01-01","periodEnd":"2026-10-28","currency":"TRY"}')"
check "  with its code" invoice_report_period_over_limit "$(errorCode)"
check "the operator reads the report, without its total" None "$(call -X POST "$G/invoice-service/v1/invoice-reports/get/$reportId" -H "Authorization: Bearer $veribanOperator" >/dev/null; json "d['payload'].get('totalAmount')" < "$work/call.json")"
check "the operator may not make one (no endpoint generate)" 403 "$(status -X POST "$G/invoice-service/v1/invoice-reports/generate" -H "Authorization: Bearer $veribanOperator" -H 'Content-Type: application/json' -d '{"periodStart":"2026-10-20","periodEnd":"2026-10-28","currency":"TRY"}')"
check "bilgera sees none of the reports of veriban" 0 "$(post "$bilgeraManager" invoice-service/v1/invoice-reports/paged-list | json "d['payload']['dataTotalCount']")"
check "the admin deletes the report (action delete)" 204 "$(status -X DELETE "$G/invoice-service/v1/invoice-reports/$reportId" -H "Authorization: Bearer $veribanAdmin")"

exit $failed
