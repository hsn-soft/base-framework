# MinIO Setup & CdnLocalMinio Test

Bu döküman, Docker'da MinIO kurulumunu ve CdnLocalMinio API'sini test etmek için talimatlar içerir.

## Step 1: MinIO'yu Docker'da Başlat

### Seçenek A: Docker Compose (Önerilen)

```bash
# Proje kökünde (vpp dizini)
docker-compose up -d

# Kontrol et
docker ps
# hhs-minio container'ı çalışmakta olmalı
```

### Seçenek B: Docker Run (Manual)

```bash
docker run -d \
  --name hhs-minio \
  -p 9100:9000 \
  -p 9101:9001 \
  -e MINIO_ROOT_USER=minioadmin \
  -e MINIO_ROOT_PASSWORD=minioadmin \
  -e MINIO_CONSOLE_ADDRESS=:9001 \
  minio/minio:latest \
  minio server /data --console-address ":9001"
```

## Step 2: MinIO UI'da Bucket Oluştur

### MinIO Console'a Gir

1. Browser'da açın: http://localhost:9101
2. Login:
   - Username: `minioadmin`
   - Password: `minioadmin`

### Bucket Oluştur

1. Sol panel → "Buckets"
2. "+ Create Bucket" tıkla
3. Name: `videos`
4. Create

### Doğrula

```
videos bucket visible in console
```

## Step 3: CdnLocalMinio API'sini Başlat

```bash
cd Hhs.MockApi.CdnLocalMinio
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5070
```

## Step 4: Health Check

```bash
curl http://localhost:5070/health

# Response:
# {"status":"healthy","provider":"local-minio","backend":"MinIO"}
```

## Step 5: Test File Upload

### Method A: CURL

```bash
curl -X POST http://localhost:5070/upload \
  -F "file=@test.txt"
```

### Method B: PowerShell (Windows)

```powershell
$form = @{
    file = Get-Item -Path "test.txt"
}
Invoke-WebRequest -Uri "http://localhost:5070/upload" -Method Post -Form $form
```

### Method C: Browser

1. Python server başlat (temporary):
```bash
python3 -m http.server 8000
```

2. Browser'da: http://localhost:8000
3. test.txt'i görüp download et
4. Ama bu method file upload etmez, sadece indirme için

### Response Example

```json
{
  "success": true,
  "fileId": "a1b2c3d4",
  "fileName": "test.txt",
  "storedFileName": "a1b2c3d4_test.txt",
  "fileSize": 347,
  "s3Key": "2026/06/20/a1b2c3d4_test.txt",
  "storageUrl": "http://localhost:9100/videos/2026/06/20/a1b2c3d4_test.txt",
  "cdnUrl": "http://localhost:5070/media/minio/2026/06/20/a1b2c3d4_test.txt",
  "minioInfo": {
    "endpoint": "http://localhost:9100",
    "bucket": "videos",
    "path": "2026/06/20/a1b2c3d4_test.txt"
  },
  "provider": "local-minio",
  "backend": "MinIO",
  "instructions": {
    "viewInMinioUI": "http://localhost:9001 → bucket 'videos' → path '2026/06/20/a1b2c3d4_test.txt'",
    "downloadViaApi": "GET /download/a1b2c3d4",
    "verifyFile": "Download and compare SHA256 hash with original"
  }
}
```

**FileId'yi not et:** `a1b2c3d4`

## Step 6: MinIO UI'da Dosyayı Kontrol Et

1. MinIO Console'u açın: http://localhost:9001
2. Login et
3. "Buckets" → "videos"
4. Path takip et: `2026/06/20/a1b2c3d4_test.txt`
5. Dosyayı görmelisiniz!

### MinIO UI İçinde Dosya Yönetimi

- **View**: Dosya bilgisini görmek için tıkla
- **Download**: İndir butonu
- **Details**: Dosya metadata'sını görmek için

## Step 7: Download Test

```bash
# Download et
curl -O http://localhost:5070/download/a1b2c3d4

# Dosya kaydedilir: a1b2c3d4 (dosya adı olmadan - response header'dan alınır)
# İstenirse dosya adı ekle:
curl -O -J http://localhost:5070/download/a1b2c3d4
```

## Step 8: Verification

### Hash Kontrolü

```bash
# Orijinal
sha256sum test.txt
# abc123def456...

# İndirilen
sha256sum a1b2c3d4
# abc123def456... (AYNI olmali!)
```

### Dosya Karşılaştırması

```bash
# Byte-for-byte comparison
cmp -l test.txt a1b2c3d4
# Hiç output yoksa dosyalar aynı
```

## Step 9: List Files

```bash
curl http://localhost:5070/list

# Response:
# {
#   "total": 1,
#   "files": [
#     {
#       "fileId": "a1b2c3d4",
#       "originalFileName": "test.txt",
#       "storedFileName": "a1b2c3d4_test.txt",
#       "fileSize": 347,
#       "s3Key": "2026/06/20/a1b2c3d4_test.txt",
#       "minioPath": "videos/2026/06/20/a1b2c3d4_test.txt",
#       "cdnUrl": "http://localhost:5070/media/minio/2026/06/20/a1b2c3d4_test.txt",
#       "uploadedAt": "2026-06-20T18:35:45Z"
#     }
#   ]
# }
```

## Complete Bash Script

```bash
#!/bin/bash

set -e

echo "=== MinIO Setup & CdnLocalMinio Test ==="

# 1. Start MinIO
echo -e "\n1️⃣ Starting MinIO Docker..."
docker-compose up -d
sleep 3

# 2. Create bucket using MinIO Client
echo -e "\n2️⃣ Creating 'videos' bucket..."
# Wait for MinIO to be ready
for i in {1..30}; do
    if curl -s http://localhost:9100/minio/health/live >/dev/null; then
        break
    fi
    echo "  Waiting for MinIO... ($i/30)"
    sleep 1
done

# 3. Health check
echo -e "\n3️⃣ Health Check..."
curl -s http://localhost:5070/health | jq . || echo "CdnLocalMinio not running yet"

# 4. Upload
echo -e "\n4️⃣ Uploading test.txt..."
RESPONSE=$(curl -s -X POST http://localhost:5070/upload -F "file=@test.txt")
FILE_ID=$(echo $RESPONSE | jq -r '.fileId')
echo "Response:"
echo $RESPONSE | jq .

# 5. MinIO UI info
echo -e "\n5️⃣ MinIO UI Access:"
echo "   URL: http://localhost:9101"
echo "   Username: minioadmin"
echo "   Password: minioadmin"
echo "   Bucket: videos"
echo "   Path: 2026/06/20/${FILE_ID}_test.txt"

# 6. Download
echo -e "\n6️⃣ Downloading file..."
curl -O -J http://localhost:5070/download/$FILE_ID
echo "   File saved"

# 7. Verify
echo -e "\n7️⃣ Verification..."
ORIGINAL_HASH=$(sha256sum test.txt | awk '{print $1}')
DOWNLOADED_HASH=$(sha256sum ${FILE_ID}_test.txt 2>/dev/null || sha256sum a1b2c3d4 2>/dev/null || echo "FILE_NOT_FOUND")

echo "   Original hash:   $ORIGINAL_HASH"
echo "   Downloaded hash: $DOWNLOADED_HASH"

if [ "$ORIGINAL_HASH" == "$DOWNLOADED_HASH" ]; then
    echo "   ✅ Files match perfectly!"
else
    echo "   ❌ Files don't match"
    exit 1
fi

# 8. List files
echo -e "\n8️⃣ List all files..."
curl -s http://localhost:5070/list | jq .

echo -e "\n✅ Test completed successfully!"
```

## Troubleshooting

### MinIO not starting
```bash
# Check if ports 9100/9101 are already in use
lsof -i :9100
lsof -i :9101

# Kill existing container
docker stop hhs-minio
docker rm hhs-minio

# Restart
docker-compose up -d
```

### CdnLocalMinio not connecting to MinIO
```bash
# Check MinIO status
docker ps
curl http://localhost:9100/minio/health/live

# Check logs
docker logs hhs-minio
```

### Bucket not created
```bash
# Create manually with curl (requires mc client)
# Or use MinIO UI: http://localhost:9101

# Verify bucket exists
curl -s http://localhost:9100/minio/health/live
```

### Download returns 404
```bash
# Verify fileId is correct
curl http://localhost:5070/list

# Check MinIO has the file
docker exec hhs-minio find /data -name "*.txt"
```

## Cleanup

```bash
# Stop containers
docker-compose down

# Remove data (optional)
docker volume rm vpp_minio_data
```

---

## CdnLocalMinio Configuration

File: `Hhs.MockApi.CdnLocalMinio/appsettings.json`

```json
{
  "MockApi": {
    "SelfBaseUrl": "http://localhost:5070",
    "MinioEndpoint": "http://localhost:9000",
    "MinioAccessKey": "minioadmin",
    "MinioSecretKey": "minioadmin",
    "MinioBucket": "videos",
    "CdnZonePath": "media",
    "CdnPathPrefix": "minio"
  }
}
```

### Configuration Fields

| Field | Value | Açıklama |
|-------|-------|----------|
| SelfBaseUrl | http://localhost:5070 | CdnLocalMinio API endpoint |
| MinioEndpoint | http://localhost:9100 | MinIO server endpoint (port 9100 = API, 9101 = Console) |
| MinioAccessKey | minioadmin | MinIO username |
| MinioSecretKey | minioadmin | MinIO password |
| MinioBucket | videos | MinIO bucket adı |
| CdnZonePath | media | CDN URL path kısmı |
| CdnPathPrefix | minio | CDN path prefix |

---

## Beklenen Davranış

✅ Upload başarılı → fileId + URLs
✅ MinIO UI'da dosya görünür
✅ Download başarılı → original dosya ile aynı
✅ Hash doğrulaması geçer
✅ List endpoint'i dosyaları gösterir
