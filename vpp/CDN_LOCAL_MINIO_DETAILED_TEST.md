# CdnLocalMinio Detailed API Documentation

Bu döküman, CdnLocalMinio API'sinin MinIO ile tam akışını adım adım gösterir.

## Architecture

```
test.txt (upload)
    ↓
CdnLocalMinio API (5070)
    ↓
MinIO (9000)
    ↓
Bucket: videos
Path: 2026/06/20/{fileId}_{fileName}
    ↓
MinIO UI'da görülebilir
    ↓
CdnLocalMinio API (download)
    ↓
test.txt (download)
```

## Step 1: File Upload

### Request

```http
POST /upload HTTP/1.1
Host: localhost:5070
Content-Type: multipart/form-data; boundary=----WebKitFormBoundary

------WebKitFormBoundary
Content-Disposition: form-data; name="file"; filename="test.txt"
Content-Type: text/plain

Bu bir test dosyasıdır.
Mock CDN API'ler bu dosyayı upload ve download yapacaklar.
...
------WebKitFormBoundary--
```

### CURL Örneği

```bash
curl -X POST http://localhost:5070/upload \
  -F "file=@test.txt"
```

### Response (Success)

```json
{
  "success": true,
  "fileId": "a1b2c3d4",
  "fileName": "test.txt",
  "storedFileName": "a1b2c3d4_test.txt",
  "fileSize": 347,
  "s3Key": "2026/06/20/a1b2c3d4_test.txt",
  "storageUrl": "http://localhost:9000/videos/2026/06/20/a1b2c3d4_test.txt",
  "cdnUrl": "http://localhost:5070/media/minio/2026/06/20/a1b2c3d4_test.txt",
  "minioInfo": {
    "endpoint": "http://localhost:9000",
    "bucket": "videos",
    "path": "2026/06/20/a1b2c3d4_test.txt"
  },
  "provider": "local-minio",
  "backend": "MinIO",
  "instructions": {
    "viewInMinioUI": "http://localhost:9000 → bucket 'videos' → path '2026/06/20/a1b2c3d4_test.txt'",
    "downloadViaApi": "GET /download/a1b2c3d4",
    "verifyFile": "Download and compare SHA256 hash with original"
  }
}
```

## Response Fields Açıklaması

| Field | Açıklama |
|-------|----------|
| **fileId** | Kısaltılmış unique ID (8 karakter) |
| **fileName** | Orijinal dosya adı (`test.txt`) |
| **storedFileName** | MinIO'da saklanan ad (`a1b2c3d4_test.txt`) |
| **fileSize** | Dosya boyutu (bytes) |
| **s3Key** | MinIO'daki path (`2026/06/20/a1b2c3d4_test.txt`) |
| **storageUrl** | MinIO'ya direct HTTP URL (internal) |
| **cdnUrl** | CdnLocalMinio API üzerinden URL (external/public) |
| **minioInfo** | MinIO endpoint, bucket, path bilgisi |

## Step 2: MinIO'da Kontrol

### MinIO UI'da Dosyayı Görüntüleme

1. Browser'da açın: `http://localhost:9000`
2. Login:
   - Username: `minioadmin`
   - Password: `minioadmin`
3. Bucket seç: `videos`
4. Path'i takip et: `2026/06/20/a1b2c3d4_test.txt`

### MinIO'da Dosya Yapısı

```
videos (bucket)
└── 2026/
    └── 06/
        └── 20/
            └── a1b2c3d4_test.txt
```

### MinIO Komut Satırı ile Kontrol

```bash
# MinIO Client (mc) kullanarak:
mc ls minio/videos/2026/06/20/

# Output:
[2026-06-20 18:35:20 UTC]   347B a1b2c3d4_test.txt

# Dosyayı indir
mc cp minio/videos/2026/06/20/a1b2c3d4_test.txt ./downloaded_test.txt
```

## Step 3: File Download

### Request

```http
GET /download/a1b2c3d4 HTTP/1.1
Host: localhost:5070
```

### CURL Örneği

```bash
curl -O http://localhost:5070/download/a1b2c3d4
# Dosya şu an disk'te: a1b2c3d4_test.txt
```

### Response

```
HTTP/1.1 200 OK
Content-Type: application/octet-stream
Content-Length: 347

[binary file content - identical to original test.txt]
```

## Step 4: Doğrulama

### Hash Kontrolü

```bash
# Orijinal dosya hash'i
sha256sum test.txt
# abc123def456...

# İndirilen dosya hash'i
sha256sum a1b2c3d4_test.txt
# abc123def456... (AYNI!)
```

### Dosya Karşılaştırması

```bash
diff test.txt a1b2c3d4_test.txt
# Eğer aynıysa output yok (perfect match)
```

## Complete Test Senaryosu

```bash
#!/bin/bash

# 1. MinIO başlat (Docker)
docker run -d --name minio \
  -p 9000:9000 -p 9001:9001 \
  -e MINIO_ROOT_USER=minioadmin \
  -e MINIO_ROOT_PASSWORD=minioadmin \
  minio/minio server /data

sleep 2

# 2. CdnLocalMinio API'ni başlat (Terminal 1)
cd Hhs.MockApi.CdnLocalMinio
dotnet run &

sleep 2

# 3. Test dosyasını upload et
echo "=== UPLOAD ==="
curl -X POST http://localhost:5070/upload \
  -F "file=@test.txt" | jq .

# fileId'yi al (örn: a1b2c3d4)
FILE_ID="a1b2c3d4"

# 4. MinIO UI'da kontrol et
echo "=== MinIO UI ==="
echo "Open: http://localhost:9001"
echo "Bucket: videos"
echo "Path: 2026/06/20/${FILE_ID}_test.txt"

# 5. Download et
echo "=== DOWNLOAD ==="
curl -O http://localhost:5070/download/${FILE_ID}

# 6. Hash kontrolü
echo "=== VERIFICATION ==="
sha256sum test.txt
sha256sum "${FILE_ID}_test.txt"
# Hashes should match!

# 7. İçerik kontrolü
echo "=== CONTENT VERIFICATION ==="
diff test.txt "${FILE_ID}_test.txt"
echo "If no output above, files are identical!"
```

## API Endpoints Summary

| Method | Endpoint | Description | Parameters |
|--------|----------|-------------|-----------|
| **POST** | `/upload` | Dosya upload et | file (multipart/form-data) |
| **GET** | `/download/{fileId}` | Dosya indir | fileId (path param) |
| **GET** | `/list` | Tüm dosyaları listele | - |
| **GET** | `/health` | API health check | - |

## Logging Output

CdnLocalMinio API aşağıdaki log'ları üretir:

### Upload Logs

```
info: Hhs.MockApi.CdnLocalMinio.MinioService[0]
      Uploading to MinIO: http://localhost:9000/videos/2026/06/20/a1b2c3d4_test.txt, FileId: a1b2c3d4, StoredName: a1b2c3d4_test.txt, Size: 347

info: Hhs.MockApi.CdnLocalMinio.MinioService[0]
      Upload successful: FileId=a1b2c3d4, StoredName=a1b2c3d4_test.txt, MinioPath=2026/06/20/a1b2c3d4_test.txt, CdnUrl=http://localhost:5070/media/minio/2026/06/20/a1b2c3d4_test.txt
```

### Download Logs

```
info: Hhs.MockApi.CdnLocalMinio.MinioService[0]
      Downloading from MinIO: a1b2c3d4, StoredName: a1b2c3d4_test.txt, MinioUrl: http://localhost:9000/videos/2026/06/20/a1b2c3d4_test.txt

info: Hhs.MockApi.CdnLocalMinio.MinioService[0]
      Download successful: a1b2c3d4, Size: 347
```

## Production vs Mock

### Şu an (Mock)
- MinIO gerçek S3-compatible storage
- Docker'da çalışan MinIO instance
- Real HTTP PUT/GET requests
- Actual MinIO UI erişim

### Gerçek Production
- AWS S3 veya MinIO in production
- S3 SDK kullanarak (şu an HTTP kullanıyoruz)
- Credentials vs IAM roles
- Aynı API interface devam eder

## Test Checklist

- [ ] MinIO Docker'da çalışıyor mı? (Port 9000)
- [ ] CdnLocalMinio API çalışıyor mı? (Port 5070)
- [ ] Health check başarılı mı? (GET /health)
- [ ] Upload başarılı mı? (fileId dönüyor mü?)
- [ ] MinIO UI'da dosya görünüyor mü?
- [ ] Download başarılı mı?
- [ ] Hash'ler eşleşiyor mu?
- [ ] Dosya içeriği birebir aynı mı?

---

**Bu döküman diğer 3 CDN'in (CdnBunnySelf, CdnBunnyS3, CdnAbc) referans noktasıdır.**

Aynı pattern:
1. Upload → Response (fileId, URLs)
2. Backend'de depo (disk/S3/Storage)
3. Download → Binary response
4. Verification → Hash match
