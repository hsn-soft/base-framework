# Mock CDN API - Complete Test Guide

Bu döküman, 4 Mock CDN API'sini test etmek için adım adım talimatlar içerir.

## Genel Bakış

Test ederken:
1. Her CDN'ye dosya **upload** edecek
2. Dosyanın nerede saklandığını görecek
3. Dosyayı **download** edecek
4. İçeriğin doğru olup olmadığını doğrulayacak

### Depo Yerleri

| CDN | Backend | Storage Lokasyonu |
|-----|---------|-------------------|
| **CdnLocalMinio (5070)** | MinIO | `media/cdn-local-minio/{date}/` |
| **CdnBunnySelf (5071)** | Disk (Self-Hosted) | `media/cdn-bunny-self/bunny/{date}/` |
| **CdnBunnyS3 (5072)** | S3 Mock (Disk) | `uploads/s3/{date}/` |
| **CdnAbc (5073)** | Storage Backend | `media/storage/{date}/` |
| **Storage (5074)** | Central Repo | `media/storage/{date}/` |

## Test Dosyası

`test.txt` dosyası zaten proje kökünde bulunuyor. İçeriği:
```
Bu bir test dosyasıdır.
Mock CDN API'ler bu dosyayı upload ve download yapacaklar.
...
```

## Step-by-Step Test

### Aşama 1: Mock API'leri Başlat (5 Terminal)

Her terminal'de şu komutları çalıştırın:

**Terminal 1 - Storage (Central Repository, Port 5074):**
```bash
cd Hhs.MockApi.Storage
dotnet run
```
Beklenen output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5074
```

**Terminal 2 - CdnLocalMinio (Port 5070):**
```bash
cd Hhs.MockApi.CdnLocalMinio
dotnet run
```

**Terminal 3 - CdnBunnySelf (Port 5071):**
```bash
cd Hhs.MockApi.CdnBunnySelf
dotnet run
```

**Terminal 4 - CdnBunnyS3 (Port 5072):**
```bash
cd Hhs.MockApi.CdnBunnyS3
dotnet run
```

**Terminal 5 - CdnAbc (Port 5073):**
```bash
cd Hhs.MockApi.CdnAbc
dotnet run
```

✅ Tüm 5 terminal'de "Now listening on" mesajı görmek important.

### Aşama 2: Tester Program'ını Çalıştır

Yeni bir terminal açıp:
```bash
cd Hhs.MockApi.Tester
dotnet run
```

## Expected Output

Programı çalıştırırken şu bilgileri göreceksiniz:

### 1. Test Dosyası Hazırlanıyor
```
📝 Test dosyası hazırlanıyor...
✅ Dosya hazır: test.txt
   Boyut: XXX bytes
   Hash: abc123def456
```

### 2. Her CDN için Test Adımları

**CdnLocalMinio Test:**
```
═══════════════════════════════════════════════════════════════════════
🔷 TEST: CdnLocalMinio
═══════════════════════════════════════════════════════════════════════
   Endpoint: http://localhost:5070
   Backend: MinIO

1️⃣ Health Check:
   ✅ Server healthy

2️⃣ File Upload:
   📤 Uploading test.txt (XXX bytes)
   ✅ Upload successful (200 OK)
   📋 FileId: a1b2c3d4e5f6g7h8
   🔗 StorageUrl: http://localhost:5070/download/a1b2c3d4e5f6g7h8
   🌐 CdnUrl: http://localhost:5070/media/minio/...

3️⃣ File Download & Verification:
   📥 Downloading from: http://localhost:5070/download/...
   ✅ Download successful (200 OK)
   📊 Downloaded: XXX bytes
   🔐 Original Hash:   abc123def456
   🔐 Download Hash:   abc123def456
   ✔️ Content Match: ✅ YES

   📁 File Storage Info:
      Expected path: media/cdn-local-minio/2026/06/20
      File pattern: a1b2c3d4e5f6*
      ✅ Found: /full/path/to/media/cdn-local-minio/2026/06/20/...
      Size: XXX bytes

   ✅ Test Passed!
```

Aynı format diğer 3 CDN için de tekrarlanacak.

### 3. Final Output
```
═══════════════════════════════════════════════════════════════════════
✅ Tüm testler tamamlandı!
```

## Troubleshooting

### "Connection Error: Unable to connect to localhost:5070"
- ❌ Mock API başlatılmadı mı?
- ✅ Çözüm: Terminal 2'deki CdnLocalMinio'yu başlat

### "Content Match: ❌ NO"
- ❌ Download edilen içerik farklı
- ✅ Çözüm: API'ler yeniden başlat, test et

### "File not found in directory"
- ❌ Depo lokasyonunda dosya yok
- ✅ Çözüm: Mock API'nin çalışmakta olduğundan emin ol, logging'i kontrol et

## Storage Lokasyonlarını Manual Kontrol

### CdnLocalMinio Storage
```bash
ls -la media/cdn-local-minio/2026/06/20/
```
Dosyaları görmek lazım: `*_test.txt` formatında

### CdnBunnySelf Storage
```bash
ls -la media/cdn-bunny-self/bunny/2026/06/20/
```

### CdnBunnyS3 Storage (Mock S3 - Local Disk)
```bash
ls -la uploads/s3/2026/06/20/
```

### Storage (Backend)
```bash
ls -la media/storage/2026/06/20/
```

## Mock vs Real Implementation

### CdnBunnyS3 - Mock Implementation
Şu an diskte kaydediliyor:
```csharp
// MOCK IMPLEMENTATION: Save to local disk instead of real S3
var mockStorageDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "uploads", "s3");
Directory.CreateDirectory(mockStorageDir);
var uploadDir = Path.Combine(mockStorageDir, dateFolder);
Directory.CreateDirectory(uploadDir);
var filePath = Path.Combine(uploadDir, $"{fileId}_{file.FileName}");
await File.WriteAllBytesAsync(filePath, fileBytes, ct);
```

Gerçek S3 implementation (commented out):
```csharp
/* REAL S3 IMPLEMENTATION (commented out for mock):
// Construct S3-compatible upload URL
var uploadUrl = $"{_options.Value.S3Endpoint}/{_options.Value.S3Bucket}/{s3Key}";

// Upload to S3 (or MinIO)
using var content = new ByteArrayContent(fileBytes);
var response = await _httpClient.PutAsync(uploadUrl, content, ct);

if (response.IsSuccessStatusCode)
{
    // Save metadata...
}
*/
```

## Test Checklist

- [ ] Tüm 5 Mock API başlatıldı
- [ ] Tester programı çalışıyor
- [ ] 4 CDN'in tamamı "✅ Server healthy" gösteriyor
- [ ] 4 CDN'in tamamında upload başarılı
- [ ] 4 CDN'in tamamında download başarılı
- [ ] 4 CDN'in tamamında "Content Match: ✅ YES"
- [ ] Storage lokasyonlarında dosyalar var
- [ ] Hash değerleri eşleşiyor

## İleri Testler (Optional)

### 1. Birden Fazla Dosya Upload
Tester program'ını birkaç kez çalıştır, her seferinde yeni fileId oluşacak.

### 2. Storage'ı Manuel Kontrol
```bash
# Tüm saklanmış dosyaları say
find media -type f -name "*.txt" | wc -l
find uploads -type f -name "*.txt" | wc -l
```

### 3. API'nin Kendi Endpoint'lerini Kontrol
```bash
# Health check
curl http://localhost:5070/health

# List endpoint (Storage API'sinde)
curl http://localhost:5074/list
```

## Sonuç

✅ Test başarılı olmuşsa:
- Tüm 4 Mock CDN doğru çalışıyor
- Upload/Download flow işliyor
- Dosyalar doğru lokasyonlarda saklanıyor
- İçerik bütünlüğü sağlanıyor

Artık bu Mock API'ler VideoGenerator ve TextNormalizer service'leri ile entegre etmeye hazırsınız!
