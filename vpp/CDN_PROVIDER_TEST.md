# 🧪 CDN Provider Integration Test Scenarios

## Flow Overview

```
VideoGeneratorService
  ↓
VideoOperationAppService.UploadAudioFileAsync / UploadVideoFileAsync
  ↓
StorageService.UploadAsync(cdnProviderKey, fileStream, filename)
  ↓
CdnProviderFactory.CreateProvider("cdn-local-minio")
  ↓
CdnHttpProvider (Storage.Type = "HttpCdn")
  ↓
POST http://localhost:5070/upload
  ↓
Hhs.MockApi.CdnLocalMinio
  ↓
MinIO (localhost:9100)
  ↓
Response: { fileId, cdnUrl, storageUrl }
```

---

## 📝 Test Scenario 1: Audio File Upload via CDN

### Prerequisites
- [ ] MinIO running on `localhost:9100`
- [ ] Hhs.MockApi.CdnLocalMinio running on `localhost:5070`
- [ ] VideoGeneratorService running on configured port
- [ ] MongoDB running
- [ ] RabbitMQ running

### Test Steps

#### Step 1: Direct Mock API Test (Baseline)
```bash
cd Hhs.MockApi.Tester
dotnet run
# Select option 1 for CdnLocalMinio detailed test
```

**Expected Results:**
- ✅ Health check passes (5070)
- ✅ File uploads successfully with fileId
- ✅ StorageUrl returned (MinIO URL)
- ✅ CdnUrl returned (http://localhost:5070/media/minio/...)
- ✅ File downloadable via StorageUrl
- ✅ File hash matches original

#### Step 2: CDN Provider Integration Test
Test via AudioFileUploadStartedEto event flow:

```
1. Publish VideoGenerationApprovedEto
   ↓ (ContentService → VideoGeneratorService)
2. Video provider generates video
   ↓
3. Publish AudioFileUploadStartedEto with:
   - LocalFilePath: path to audio file
   - AudioCdnProviderKey: "cdn-local-minio" (or let it default)
   ↓
4. VideoOperationAppService.UploadAudioFileAsync:
   - Opens file stream
   - Calls StorageService.UploadAsync("cdn-local-minio", stream, filename)
   ↓
5. StorageService:
   - Creates CdnHttpProvider via factory
   - Calls provider.UploadAsync(stream, filename)
   ↓
6. CdnHttpProvider:
   - POST to http://localhost:5070/upload
   - Receives fileId, storageUrl, cdnUrl
   ↓
7. AudioFileUploadCompletedEto published with:
   - StorageUrl: http://localhost:9100/videos/...
   - (+ AudioCdnUrl in request object)
```

---

## 🎯 Verification Checklist

### Mock API Level
- [ ] /health endpoint responds with status "healthy"
- [ ] /upload accepts multipart file form data
- [ ] Response includes: fileId, fileName, storageUrl, cdnUrl, s3Key
- [ ] File actually stored in MinIO (check via MinIO console)
- [ ] /download/{fileId} returns original file
- [ ] Downloaded file hash matches original

### CDN Provider Level
- [ ] CdnLocalMinioCdnSettings loads correctly from appsettings
- [ ] CdnProviderFactory recognizes "httpcdn" type
- [ ] Creates CdnHttpProvider instance
- [ ] CdnHttpProvider calls correct endpoint

### Storage Service Level
- [ ] StorageService.UploadAsync called with correct parameters
- [ ] Returns (storageUrl, cdnUrl) tuple
- [ ] Both URLs are accessible

### Application Level (VideoGeneratorService)
- [ ] VideoOperationAppService.UploadAudioFileAsync succeeds
- [ ] AudioRequest.AudioStorageUrl saved correctly
- [ ] AudioRequest.AudioCdnUrl saved correctly
- [ ] AudioFileUploadCompletedEto published with storageUrl
- [ ] Logs show successful upload

### MinIO Level
- [ ] File exists in bucket at expected path
- [ ] File content matches original
- [ ] Can view via MinIO Console UI
- [ ] Metadata correct (size, upload time)

---

## 📊 Expected URLs

### CdnLocalMinio Configuration
```json
{
  "BaseUrl": "http://localhost:5070",
  "ZonePath": "media",
  "PathPrefix": "minio"
}
```

### Example URLs After Upload
- **Mock API Upload Endpoint:** `POST http://localhost:5070/upload`
- **Mock API Download:** `GET http://localhost:5070/download/abc12345`
- **StorageUrl:** `http://localhost:9100/videos/2024/06/20/abc12345_filename.mp3`
- **CdnUrl:** `http://localhost:5070/media/minio/videos/2024/06/20/abc12345_filename.mp3`
- **MinIO Console:** `http://localhost:9101`

---

## 🚀 Running Services

```bash
# Terminal 1: MinIO
docker run -it --rm \
  -p 9100:9000 \
  -p 9101:9001 \
  -e MINIO_ROOT_USER=minioadmin \
  -e MINIO_ROOT_PASSWORD=minioadmin \
  -v /tmp/minio:/data \
  minio/minio server /data

# Terminal 2: MongoDB
docker run -it --rm -p 27017:27017 mongo:latest

# Terminal 3: RabbitMQ
docker run -it --rm -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Terminal 4: Mock CDN Local Minio
cd Hhs.MockApi.CdnLocalMinio
dotnet run

# Terminal 5: Mock CDN APIs (other CDN providers)
# cd Hhs.MockApi.CdnBunnySelf && dotnet run
# cd Hhs.MockApi.CdnBunnyS3 && dotnet run
# cd Hhs.MockApi.CdnAbc && dotnet run

# Terminal 6: VideoGeneratorService
cd Hhs.VideoGeneratorService
dotnet run

# Terminal 7: ContentService
cd Hhs.ContentService
dotnet run

# Terminal 8: Tester
cd Hhs.MockApi.Tester
dotnet run
```

---

## 📋 Log Verification

Look for these log messages in VideoGeneratorService console:

```
[Information] Uploading file 'audio.mp3' to CDN provider 'cdn-local-minio'
[Information] File uploaded successfully. StorageUrl: ..., CdnUrl: ...
[Information] File downloaded successfully from CDN provider 'cdn-local-minio'
```

---

## 🔄 Retry Behavior

If upload fails:
1. Automatic retry with configured backoff
2. Max retry count respected
3. Failure event published after exhausting retries
4. Error logged with full exception details
