# CDN Mock API Test Guide

Bu döküman, 5 Mock CDN/Storage API'sini test etmek için step-by-step talimatlar içerir.

## Architecture

```
VideoGenerator/TextNormalizer
  ↓ (HTTP Requests)
  │
  ├─ Hhs.MockApi.CdnLocalMinio   (Port 5070) → MinIO backend
  ├─ Hhs.MockApi.CdnBunnySelf    (Port 5071) → Disk backend (self-hosted)
  ├─ Hhs.MockApi.CdnBunnyS3      (Port 5072) → S3/MinIO backend
  └─ Hhs.MockApi.CdnAbc          (Port 5073) → Delegates to Storage
     └─ Hhs.MockApi.Storage      (Port 5074) → Central repository
```

## Test Adımları

### 1. Mock API'leri Başlat (5 Ayrı Terminal)

**Terminal 1 - Storage (Central Repository):**
```bash
cd Hhs.MockApi.Storage
dotnet run
```
Output: `Now listening on: http://localhost:5074`

**Terminal 2 - CdnLocalMinio:**
```bash
cd Hhs.MockApi.CdnLocalMinio
dotnet run
```
Output: `Now listening on: http://localhost:5070`

**Terminal 3 - CdnBunnySelf:**
```bash
cd Hhs.MockApi.CdnBunnySelf
dotnet run
```
Output: `Now listening on: http://localhost:5071`

**Terminal 4 - CdnBunnyS3:**
```bash
cd Hhs.MockApi.CdnBunnyS3
dotnet run
```
Output: `Now listening on: http://localhost:5072`

**Terminal 5 - CdnAbc:**
```bash
cd Hhs.MockApi.CdnAbc
dotnet run
```
Output: `Now listening on: http://localhost:5073`

### 2. Test Program Çalıştır

Ayrı bir terminal'de:
```bash
cd Hhs.MockApi.Tester
dotnet run
```

Bu program otomatik olarak:
1. `abc.txt` test dosyasını oluşturur
2. Her CDN API'ye upload eder
3. Response'ları gösterir (fileId, URLs, vb.)
4. Download ederek dosyayı doğrular
5. Sorunları bulursa rapor eder

## API Endpoints

### Upload Endpoint

**Request:**
```bash
POST http://localhost:5073/upload
Content-Type: multipart/form-data

file: abc.txt (binary)
```

**Response (Success):**
```json
{
  "success": true,
  "fileId": "a1b2c3d4e5f6...",
  "fileName": "abc.txt",
  "fileSize": 1024,
  "storageUrl": "http://localhost:5074/download/a1b2c3d4e5f6...",
  "cdnUrl": "http://localhost:5073/files/abc/a1b2c3d4e5f6...",
  "provider": "abc"
}
```

### Download Endpoint

**Request:**
```bash
GET http://localhost:5073/download/{fileId}
```

**Response:**
- Status: 200 OK
- Body: Binary file content
- Header: `Content-Type: application/octet-stream`

### Health Check

**Request:**
```bash
GET http://localhost:5073/health
```

**Response:**
```json
{
  "status": "healthy",
  "provider": "abc"
}
```

## Manual Test (Curl)

### Upload ile Test:

```bash
# CdnAbc'ye upload
curl -X POST \
  -F "file=@abc.txt" \
  http://localhost:5073/upload

# Output:
# {
#   "success": true,
#   "fileId": "xyz123",
#   "storageUrl": "http://localhost:5074/download/xyz123",
#   ...
# }
```

### Download ile Test:

```bash
# Dönen URL'den download et
curl -O http://localhost:5074/download/xyz123

# Ya da CdnAbc'nin URL'sinden
curl -O http://localhost:5073/download/xyz123
```

## Expected Behavior

### CdnLocalMinio (5070)
- ✅ File upload → Internal MinIO storage
- ✅ File organized: `media/minio/2024/06/20/fileId_filename`
- ✅ Returns: fileId, storageUrl, cdnUrl
- ✅ Download works from returned URL

### CdnBunnySelf (5071)
- ✅ File upload → Local disk storage
- ✅ File organized: `media/bunny/2024/06/20/fileId_filename`
- ✅ Zone/path logic: `/media/bunny/{date}/{filename}`
- ✅ Download works with zone-aware path

### CdnBunnyS3 (5072)
- ✅ File upload → S3-compatible endpoint (MinIO at 9000)
- ✅ File organized: `2024/06/20/fileId_filename`
- ✅ Requires S3 credentials (appsettings)
- ✅ Download from S3 endpoint

### CdnAbc (5073) → Storage (5074)
- ✅ File upload → Delegates to Storage API
- ✅ File stored in: `media/storage/{date}/{fileId}_filename`
- ✅ Returns: fileId, storageUrl (from 5074), cdnUrl (from 5073)
- ✅ CdnAbc acts as proxy/gateway

### Storage (5074)
- ✅ Central file repository
- ✅ Simple upload/download/list
- ✅ Used by CdnAbc as backend

## Troubleshooting

### Port Already in Use
```bash
# Kill process on port (MacOS/Linux)
lsof -ti:5073 | xargs kill -9

# Windows
netstat -ano | findstr :5073
taskkill /PID {PID} /F
```

### Connection Refused
- Tüm 5 API'nin başlatılı olup olmadığını kontrol et
- Firewall ayarlarını kontrol et
- `localhost` yerine `127.0.0.1` dene

### File Not Found on Download
- Upload'ın başarılı olduğundan emin ol (200 OK)
- FileId doğru olup olmadığını kontrol et
- Storage'ın çalışmakta olduğundan emin ol (CdnAbc test'inde)

## Sonraki Adımlar

✅ Mock API'ler test edildikten sonra:
1. Main projects'e (VideoGenerator, TextNormalizer) entegre et
2. Step 7: Service Integration (handlers)
3. Step 8: Build & Test
4. Git commit
