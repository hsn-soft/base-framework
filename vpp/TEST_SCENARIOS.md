# Content Service Test Scenarios

## Overview
4 different provider combinations to test the complete microservice ecosystem.

## Senaryo 1: Fast Outline + Fast Audio + External Video
```
OUTLINE: openai (ABC) - Immediate (5s)
AUDIO: audio-def (DEF) - Immediate (3s)  
VIDEO: video-external - Polling (120s)
```

**Expected Flow:**
1. POST /customer-contents with senaryo1 config
2. ContentService creates content
3. Publishes CustomerNormalizeRequestCreated event
4. TextNormalizer receives, calls OutlineAbc MockAPI (waits 5s)
5. Gets outline immediately, publishes NormalizerResultPublished
6. ContentService approves and sends to VideoGenerator
7. VideoGenerator initiates audio generation (3s)
8. Audio file downloaded and stored
9. VideoGenerator calls VideoExternalAudio MockAPI
10. Returns tracking ID, VideoGenerator polls (every 5s until 120s)
11. Video complete, downloaded, stored
12. ContentService receives final video URL

**Total Time:** ~128 seconds

---

## Senaryo 2: Fast Outline + Slow Audio + External Video
```
OUTLINE: openai (ABC) - Immediate (5s)
AUDIO: audio-ghj (GHJ) - Polling (60s)
VIDEO: video-external - Polling (120s)
```

**Expected Flow:**
- Same as Senaryo 1 but AudioGhj takes 60s to process
- VideoGenerator waits for audio completion before triggering video

**Total Time:** ~185 seconds

---

## Senaryo 3: Slow Outline + Slow Audio + External Video
```
OUTLINE: custom-xyz (XYZ) - Polling (30s)
AUDIO: audio-ghj (GHJ) - Polling (60s)
VIDEO: video-external - Polling (120s)
```

**Expected Flow:**
- TextNormalizer gets tracking ID from OutlineXyz, polls
- After 30s gets outline
- Then proceeds with audio & video (same as Senaryo 2)

**Total Time:** ~210 seconds

---

## Senaryo 4: Fast Outline + No Audio + Internal Video
```
OUTLINE: openai (ABC) - Immediate (5s)
AUDIO: null (No external audio)
VIDEO: video-internal - Polling (120s)
```

**Expected Flow:**
1. POST /customer-contents with senaryo4 config
2. ContentService creates content
3. TextNormalizer gets outline from OutlineAbc (5s)
4. NormalizerResultPublished (no audio generation needed)
5. ContentService sends to VideoGenerator with outline data ONLY
6. VideoGenerator skips audio generation step
7. Calls VideoInternalAudio MockAPI with outline data
8. VideoInternalAudio generates both outline-based audio AND video internally
9. Returns tracking ID, VideoGenerator polls (every 5s until 120s)
10. Video complete with internal audio

**Total Time:** ~125 seconds
**Key Difference:** AudioProviderKey is null, VideoGenerator detects this and skips audio steps

---

## Testing Instructions

### Start All Services
```bash
# Terminal 1: Content Service
cd vpp
dotnet run --project Hhs.ContentService/Hhs.ContentService.csproj

# Terminal 2: Text Normalizer
ASPNETCORE_URLS=http://localhost:5001 dotnet run --project Hhs.TextNormalizerService/Hhs.TextNormalizerService.csproj

# Terminal 3: Video Generator
ASPNETCORE_URLS=http://localhost:5002 dotnet run --project Hhs.VideoGeneratorService/Hhs.VideoGeneratorService.csproj

# Terminal 4-9: Mock APIs (in separate terminals)
ASPNETCORE_URLS=http://localhost:5010 dotnet run --project Hhs.MockApi.OutlineAbc/Hhs.MockApi.OutlineAbc.csproj
ASPNETCORE_URLS=http://localhost:5011 dotnet run --project Hhs.MockApi.OutlineXyz/Hhs.MockApi.OutlineXyz.csproj
ASPNETCORE_URLS=http://localhost:5020 dotnet run --project Hhs.MockApi.AudioDef/Hhs.MockApi.AudioDef.csproj
ASPNETCORE_URLS=http://localhost:5021 dotnet run --project Hhs.MockApi.AudioGhj/Hhs.MockApi.AudioGhj.csproj
ASPNETCORE_URLS=http://localhost:5030 dotnet run --project Hhs.MockApi.VideoInternalAudio/Hhs.MockApi.VideoInternalAudio.csproj
ASPNETCORE_URLS=http://localhost:5031 dotnet run --project Hhs.MockApi.VideoExternalAudio/Hhs.MockApi.VideoExternalAudio.csproj
```

### Run Tests
In VS Code, open `Hhs.ContentService/examples.http` and:
1. Click "Send Request" on Senaryo 1 to Senaryo 4
2. Get contentId from response
3. Monitor logs in each service to see the flow
4. After expected time, check ContentService for completion

### Verification
- Monitor event flow through RabbitMQ logs
- Check final status with GET endpoints (if available)
- Verify content status transitions
- Confirm video URL in final response

---

## Key Testing Points

✅ **Provider Selection** - Correct provider called based on request
✅ **Event Flow** - Events published in correct order
✅ **Polling** - Background schedulers poll at correct intervals
✅ **Timeout Handling** - Proper timeout after max retries
✅ **Idempotency** - No duplicate processing on event retries
✅ **State Management** - Content status reflects current operation
✅ **Error Handling** - Graceful failure with error messages
✅ **Concurrency** - Multiple requests can run simultaneously
