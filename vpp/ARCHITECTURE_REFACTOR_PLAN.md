# CDN-Based Upload Architecture Refactor

## 🔴 ESKI FLOW (Direct Storage)
```
StartAudioProviderRequestAsync
├─ provider.CreateAsync() → ProviderFileUrl
├─ DownloadAndUploadToStorageAsync() // MONOLITHIC
│  ├─ HTTP GET provider file
│  └─ HTTP POST storage/upload
└─ AudioProviderCompletedEto (with StorageUrl)
```

**Problem:** 
- Mikroservice Storage mock'u hardcoded bilir
- Download ve Upload ayrı işlemler değil
- CDN provider'ı uygulayamaz

---

## 🟢 YENİ FLOW (CDN-Based)
```
StartAudioProviderRequestAsync
├─ provider.CreateAsync() → ProviderFileUrl
├─ DownloadFileAsync() // SADECE DOWNLOAD
│  └─ HTTP GET provider file → LocalPath save
├─ Update AudioRequest.LocalFilePath
├─ AudioFileDownloadCompletedEto publish
│
└─ AudioFileDownloadCompletedEtoHandler
   ├─ Get CDN Provider Key from appsettings
   ├─ Resolve CDN Provider (ICdnProvider)
   ├─ UploadFileAsync() // SADECE UPLOAD
   │  └─ HTTP POST cdnProvider/upload
   ├─ Update AudioRequest.CdnUrl
   └─ AudioFileUploadCompletedEto publish
```

---

## 📋 YAPILMASI GEREKENLER

### 1. AudioRequest Entity Değişiklikleri

**Add Fields:**
```csharp
public class AudioRequest
{
    // ... existing fields ...
    
    // NEW: Download yapıldıktan sonra local path
    public string? LocalFilePath { get; set; }
    
    // NEW: CDN upload yapıldıktan sonra final URL
    public string? CdnFileUrl { get; set; }
    
    // NEW: Hangi CDN provider'ı kullanalı
    public string? CdnProviderKey { get; set; }
}
```

---

### 2. StartAudioProviderRequestAsync - DOWNLOAD ONLY

**OLD (lines 255-277):**
```csharp
// ❌ REMOVE THIS
var mockStorageUrl = await DownloadAndUploadToStorageAsync(
    response.ProviderFileUrl,
    localFileName,
    cancellationToken);

audioRequest.AudioProviderUrl = mockStorageUrl;
audioRequest.Status = StatusNames.AudioProviderCompleted;
```

**NEW:**
```csharp
// ✅ ONLY DOWNLOAD
var localFileName = $"local_audio_{audioRequest.Id:N}.mp3";
var localFilePath = await DownloadFileAsync(
    response.ProviderFileUrl,
    localFileName,
    cancellationToken);

// Save local path
audioRequest.LocalFilePath = localFilePath;
audioRequest.Status = StatusNames.AudioFileDownloading; // NEW STATUS
audioRequest.CurrentStep = EventNames.AudioFileDownloadStarted;
audioRequest.UpdatedAtUtc = DateTime.UtcNow;

await ReplaceAudioAsync(audioRequest, cancellationToken);

// ✅ PUBLISH DOWNLOAD EVENT
await eventBus.PublishAsync(new AudioFileDownloadCompletedEto
{
    RefContentId = audioRequest.RefContentId,
    RefContentType = @event.RefContentType,
    CorrelationId = @event.CorrelationId,
    VideoRequestId = audioRequest.VideoRequestId,
    AudioRequestId = audioRequest.Id,
    LocalFilePath = localFilePath,
    ProviderFileUrl = response.ProviderFileUrl
}, cancellationToken);
```

---

### 3. New Handler: AudioFileDownloadCompletedEtoHandler

**NEW Handler:**
```csharp
public async Task HandleAudioFileDownloadCompletedAsync(
    AudioFileDownloadCompletedEto @event,
    CancellationToken cancellationToken)
{
    var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);
    
    // Get CDN Provider Key from SubscriptionScopeRegistry
    var cdnProviderKey = SubscriptionScopeRegistry.GetCdnProviderKey(audioRequest.ScopeKey);
    if (string.IsNullOrWhiteSpace(cdnProviderKey))
        cdnProviderKey = "default-cdn"; // Fallback
    
    // Resolve CDN Provider
    var cdnProvider = cdnProviderResolver.Resolve(cdnProviderKey);
    
    try
    {
        audioRequest.CdnProviderKey = cdnProviderKey;
        audioRequest.Status = StatusNames.AudioFileUploading; // NEW STATUS
        audioRequest.CurrentStep = EventNames.AudioFileUploadStarted;
        audioRequest.UpdatedAtUtc = DateTime.UtcNow;
        
        await ReplaceAudioAsync(audioRequest, cancellationToken);
        
        // ✅ UPLOAD TO CDN
        var fileContent = await System.IO.File.ReadAllBytesAsync(
            @event.LocalFilePath, 
            cancellationToken);
        
        var fileName = Path.GetFileName(@event.LocalFilePath);
        var cdnResponse = await cdnProvider.UploadAsync(
            fileContent,
            fileName,
            cancellationToken);
        
        // Save CDN URL
        audioRequest.CdnFileUrl = cdnResponse.CdnUrl;
        audioRequest.Status = StatusNames.AudioProviderCompleted;
        audioRequest.CurrentStep = EventNames.AudioProviderCompleted;
        audioRequest.UpdatedAtUtc = DateTime.UtcNow;
        
        await ReplaceAudioAsync(audioRequest, cancellationToken);
        
        // ✅ PUBLISH UPLOAD COMPLETE EVENT
        await eventBus.PublishAsync(new AudioFileUploadCompletedEto
        {
            RefContentId = audioRequest.RefContentId,
            RefContentType = @event.RefContentType,
            CorrelationId = @event.CorrelationId,
            VideoRequestId = audioRequest.VideoRequestId,
            AudioRequestId = audioRequest.Id,
            CdnFileUrl = cdnResponse.CdnUrl,
            CdnProviderKey = cdnProviderKey
        }, cancellationToken);
    }
    catch (Exception ex)
    {
        // Error handling
        audioRequest.Status = StatusNames.AudioFileUploadFailed;
        audioRequest.LastError = ex.Message;
        await ReplaceAudioAsync(audioRequest, cancellationToken);
        throw;
    }
}
```

---

### 4. NEW Helper Methods

**DownloadFileAsync (ONLY DOWNLOAD):**
```csharp
private async Task<string> DownloadFileAsync(
    string downloadUrl,
    string localFileName,
    CancellationToken cancellationToken)
{
    try
    {
        var fileContent = await httpClient.GetByteArrayAsync(downloadUrl, cancellationToken);
        
        // Save to local temp path
        var tempPath = Path.Combine(Path.GetTempPath(), "hhs_media");
        Directory.CreateDirectory(tempPath);
        
        var filePath = Path.Combine(tempPath, localFileName);
        await System.IO.File.WriteAllBytesAsync(filePath, fileContent, cancellationToken);
        
        logger.LogInformation($"✅ Downloaded file to {filePath}");
        return filePath;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, $"❌ Failed to download file from {downloadUrl}");
        throw;
    }
}
```

**REMOVE:** `DownloadAndUploadToStorageAsync` (OLD monolithic method)

---

### 5. SubscriptionScopeRegistry Enhancement

**Add CDN Provider Key:**
```csharp
public class SubscriptionScope
{
    public string OutlineProviderKey { get; set; } = default!;
    public string? AudioProviderKey { get; set; }
    public string VideoProviderKey { get; set; } = default!;
    public string? CdnProviderKey { get; set; } = "CdnLocalMinio"; // NEW
}

// In Initialize():
Add("scenario-001", new SubscriptionScope(
    outlineProviderKey: "outline-fast",
    audioProviderKey: "audio-quick",
    videoProviderKey: "video-fast-external",
    cdnProviderKey: "CdnLocalMinio" // NEW
));
```

**Add Method:**
```csharp
public static string? GetCdnProviderKey(string scopeKey)
{
    return GetScope(scopeKey)?.CdnProviderKey;
}
```

---

### 6. Event DTOs

**NEW: AudioFileDownloadCompletedEto**
```csharp
public sealed class AudioFileDownloadCompletedEto
{
    public Guid AudioRequestId { get; set; }
    public Guid VideoRequestId { get; set; }
    public Guid RefContentId { get; set; }
    public ContentType RefContentType { get; set; }
    public string CorrelationId { get; set; } = default!;
    public string LocalFilePath { get; set; } = default!; // NEW
    public string ProviderFileUrl { get; set; } = default!;
}
```

**NEW: AudioFileUploadCompletedEto**
```csharp
public sealed class AudioFileUploadCompletedEto
{
    public Guid AudioRequestId { get; set; }
    public Guid VideoRequestId { get; set; }
    public Guid RefContentId { get; set; }
    public ContentType RefContentType { get; set; }
    public string CorrelationId { get; set; } = default!;
    public string CdnFileUrl { get; set; } = default!; // NEW
    public string CdnProviderKey { get; set; } = default!;
}
```

---

## 📊 YENI EVENT FLOW

```
AudioProviderRequestStartedEto
│
├─ StartAudioProviderRequestAsync()
│  ├─ provider.CreateAsync()
│  ├─ DownloadFileAsync() → LocalPath
│  ├─ Update: LocalFilePath
│  └─ AudioFileDownloadCompletedEto publish ✅
│
└─ AudioFileDownloadCompletedEtoHandler()
   ├─ Get CDN Provider Key from SubscriptionScopeRegistry
   ├─ Resolve CDN Provider (appsettings'den)
   ├─ Upload file to CDN
   ├─ Update: CdnFileUrl
   └─ AudioFileUploadCompletedEto publish ✅
```

---

## 🎬 VIDEO PROVIDER - SAME PATTERN

**StartVideoProviderRequestAsync** - SAME REFACTOR:
1. Download video file → LocalFilePath
2. Publish VideoFileDownloadCompletedEto
3. Handler: Resolve CDN, Upload, Publish VideoFileUploadCompletedEto

**Flow:**
```
VideoFileDownloadCompletedEto
│
└─ VideoFileDownloadCompletedEtoHandler()
   ├─ Get CDN Provider (appsettings)
   ├─ Upload to CDN
   └─ VideoFileUploadCompletedEto publish ✅
```

---

## 🔑 KEY CHANGES SUMMARY

| Area | OLD | NEW |
|------|-----|-----|
| **Download + Upload** | One function | Two separate handlers |
| **Storage Location** | Hardcoded MockApi.Storage | CDN from appsettings |
| **Provider Resolution** | N/A | From SubscriptionScopeRegistry + appsettings |
| **Entity Fields** | LocalFilePath ❌ | LocalFilePath ✅, CdnFileUrl ✅ |
| **Events** | AudioProviderCompletedEto | AudioFileDownloadCompletedEto + AudioFileUploadCompletedEto |
| **Microservice Knowledge** | Storage mock API | CDN provider interface |

---

## ✅ EXECUTION STEPS

1. ✏️ Add fields to AudioRequest & VideoRequest entity
2. 🗑️ Remove `DownloadAndUploadToStorageAsync()` method
3. ➕ Add `DownloadFileAsync()` helper
4. 🔧 Refactor `StartAudioProviderRequestAsync()` - DOWNLOAD ONLY
5. ➕ Add `AudioFileDownloadCompletedEtoHandler` - UPLOAD + CDN
6. 🔧 Refactor `StartVideoProviderRequestAsync()` - DOWNLOAD ONLY
7. ➕ Add `VideoFileDownloadCompletedEtoHandler` - UPLOAD + CDN
8. 📝 Update SubscriptionScopeRegistry - Add CDN keys
9. 📦 Update Program.cs - Register new handlers
10. 🧪 Test complete flow with CDN provider
