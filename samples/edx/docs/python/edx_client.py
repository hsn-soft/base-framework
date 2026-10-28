#!/usr/bin/env python3
"""What a customer does against Edx over REST, with the standard library only: sign in, zip one invoice file, hash the zip, upload it, read the queue.

    python3 edx_client.py upload <file> <invoice-number> [invoice-uuid]
    python3 edx_client.py queue <invoice-uuid>

Settings (environment): EDX_GATEWAY (default http://localhost:5600), EDX_TENANT, EDX_USER, EDX_PASSWORD.
"""
import hashlib
import io
import json
import os
import sys
import uuid
import zipfile
import urllib.error
import urllib.request

GATEWAY = os.environ.get("EDX_GATEWAY", "http://localhost:5600")
SOURCE_TYPES = {".xml": "Xml", ".txt": "Txt", ".csv": "Csv", ".xls": "Xls", ".xlsx": "Xls"}


def call(method, path, body=None, token=None, headers=None, raw=False):
    """One call to the gateway; returns (status, payload) for JSON, (status, bytes) when raw."""
    request = urllib.request.Request(f"{GATEWAY}/{path}", method=method, data=body)
    if token:
        request.add_header("Authorization", f"Bearer {token}")
    for name, value in (headers or {}).items():
        request.add_header(name, value)
    try:
        with urllib.request.urlopen(request) as response:
            data = response.read()
            status = response.status
    except urllib.error.HTTPError as error:
        data = error.read()
        status = error.code
    if raw:
        return status, data
    if not data:
        return status, None
    try:
        return status, json.loads(data).get("payload", json.loads(data))
    except ValueError:
        return status, data.decode("utf-8", "replace")


def post_json(path, body, token=None):
    return call("POST", path, json.dumps(body).encode(), token, {"Content-Type": "application/json"})


def login(tenant, user, password):
    status, payload = post_json("auth-server/v1/auth/login", {"tenant": tenant, "username": user, "password": password})
    if status != 200:
        raise SystemExit(f"login failed ({status})")
    return payload["accessToken"]


def zip_bytes(name, content):
    """A zip with the one file in it, made in memory."""
    buffer = io.BytesIO()
    with zipfile.ZipFile(buffer, "w", zipfile.ZIP_DEFLATED) as archive:
        archive.writestr(name, content)
    return buffer.getvalue()


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def upload(token, file_name, content, number, invoice_id=None, source_type=None, zip_name=None, route="intake-service/v1/archives/upload"):
    """Zips the file, hashes the zip and uploads it as a multipart form; returns (status, payload)."""
    invoice_id = invoice_id or str(uuid.uuid4())
    source_type = source_type or SOURCE_TYPES[os.path.splitext(file_name)[1].lower()]
    archive = zip_bytes(file_name, content)
    boundary = uuid.uuid4().hex
    fields = {"invoiceId": invoice_id, "invoiceNumber": number, "fileHash": sha256(archive), "sourceType": source_type}
    parts = []
    for key, value in fields.items():
        parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{key}"\r\n\r\n{value}\r\n'.encode())
    parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="{zip_name or file_name + ".zip"}"\r\nContent-Type: application/zip\r\n\r\n'.encode() + archive + b"\r\n")
    parts.append(f"--{boundary}--\r\n".encode())
    status, payload = call("POST", route, b"".join(parts), token, {"Content-Type": f"multipart/form-data; boundary={boundary}"})
    return status, payload, invoice_id


def queue_of(token, invoice_id):
    status, payload = post_json("intake-service/v1/queue/paged-list", {"pageNumber": 1, "maxResultCount": 50}, token)
    return [item for item in (payload or {}).get("items", []) if item["invoiceId"] == invoice_id]


if __name__ == "__main__":
    if len(sys.argv) < 3:
        raise SystemExit(__doc__)
    session = login(os.environ["EDX_TENANT"], os.environ["EDX_USER"], os.environ["EDX_PASSWORD"])
    if sys.argv[1] == "upload":
        path = sys.argv[2]
        with open(path, "rb") as handle:
            code, answer, invoice = upload(session, os.path.basename(path), handle.read(), sys.argv[3], sys.argv[4] if len(sys.argv) > 4 else None)
        print(code, invoice, json.dumps(answer, indent=2))
    elif sys.argv[1] == "queue":
        print(json.dumps(queue_of(session, sys.argv[2]), indent=2))
