# Test Scenarios - 8 Provider Matrix

## Provider Definitions

### Outline Providers (2)
| Key | Type | Execution | Response Time |
|-----|------|-----------|----------------|
| outline-fast | ImmediateResult | Synchronous | ~5s |
| outline-detailed | AsyncPolling | Polling | ~30s window |

### Audio Providers (2)
| Key | Type | Execution | Response Time |
|-----|------|-----------|----------------|
| audio-quick | ImmediateResult | Synchronous | ~3s |
| audio-hq | AsyncPolling | Polling | ~60s window |

### Video Providers (4)
| Key | Type | Execution | Audio Input | Response Time |
|-----|------|-----------|-------------|----------------|
| video-fast | ImmediateResult | Sync | AudioUrlListRequired | ~5s |
| video-sync | ImmediateResult | Sync | AudioFileRequired | ~5s |
| video-cloud | AsyncPolling | Polling | AudioUrlListRequired | ~120s window |
| video-pro | AsyncPolling | Polling | AudioFileRequired | ~120s window |

---

## Test Scenarios (8 Total)

### SCENARIO 1: All Immediate (Fast Path)
**Providers**: outline-fast + audio-quick + video-fast
- **Flow**: Immediate → Immediate → Immediate
- **Audio Input**: URLs (quick generates file, fast accepts URLs)
- **Expected Total**: ~13s
- **Key Test**: All synchronous operations, database state immediate
- **Database Check**:
  - ContentService: All statuses should be completed
  - TextNormalizer: Outline result should be ready
  - VideoGenerator: Audio and video results ready

---

### SCENARIO 2: Fast Outline + Quick Audio + Sync Video (Files)
**Providers**: outline-fast + audio-quick + video-sync
- **Flow**: Immediate → Immediate → Immediate
- **Audio Input**: Files (quick generates file, sync requires files)
- **Expected Total**: ~13s
- **Key Test**: File passing between services
- **Database Check**:
  - AudioRequestId referenced in customer_contents
  - Audio file URL in database

---

### SCENARIO 3: Fast Outline + Quick Audio + Cloud Video
**Providers**: outline-fast + audio-quick + video-cloud
- **Flow**: Immediate → Immediate → Polling
- **Audio Input**: URLs (immediate ready, cloud accepts URLs)
- **Expected Total**: ~128s
- **Key Test**: Transition from immediate to polling
- **Database Check**:
  - VideoStatus tracks progress
  - VideoRequestId with ProviderTrackId

---

### SCENARIO 4: Fast Outline + Quick Audio + Pro Video
**Providers**: outline-fast + audio-quick + video-pro
- **Flow**: Immediate → Immediate → Polling
- **Audio Input**: Files (quick generates file, pro requires files)
- **Expected Total**: ~128s
- **Key Test**: File passing + polling orchestration
- **Database Check**:
  - Audio file URL stored
  - Video polling progress tracked

---

### SCENARIO 5: Fast Outline + HQ Audio + Cloud Video
**Providers**: outline-fast + audio-hq + video-cloud
- **Flow**: Immediate → Polling → Polling
- **Audio Input**: URLs (polling generates, cloud accepts URLs)
- **Expected Total**: ~188s
- **Key Test**: Parallel polling for audio and video
- **Database Check**:
  - NormalizeStatus + VideoStatus both polling
  - Audio file ready before video starts

---

### SCENARIO 6: Fast Outline + HQ Audio + Pro Video
**Providers**: outline-fast + audio-hq + video-pro
- **Flow**: Immediate → Polling → Polling
- **Audio Input**: Files (polling generates, pro requires files)
- **Expected Total**: ~188s
- **Key Test**: Audio file passing from polling to next polling
- **Database Check**:
  - Dependent polling: video waits for audio completion

---

### SCENARIO 7: Detailed Outline + HQ Audio + Cloud Video
**Providers**: outline-detailed + audio-hq + video-cloud
- **Flow**: Polling → Polling → Polling
- **Audio Input**: URLs
- **Expected Total**: ~210s
- **Key Test**: All async operations with proper sequencing
- **Database Check**:
  - All three statuses in polling state
  - Proper dependency ordering

---

### SCENARIO 8: Detailed Outline + HQ Audio + Pro Video
**Providers**: outline-detailed + audio-hq + video-pro
- **Flow**: Polling → Polling → Polling
- **Audio Input**: Files
- **Expected Total**: ~210s
- **Key Test**: Full async pipeline with file passing
- **Database Check**:
  - Complete async workflow with all dependencies

---

## Event Flow & Retry Testing

### Expected Event Sequence (Scenario 1 - All Immediate)
1. **ContentService**: CreateCustomerContentRequest → CustomerNormalizeRequested
2. **TextNormalizerService**: Receives event → OutlineProviderRequested
3. **OutlineFastProvider**: Immediate response → NormalizerResultPublished
4. **ContentService**: Receives result → VideoGenerationApproved
5. **VideoGeneratorService**: Receives event → AudioProviderRequested (if audio needed)
6. **AudioQuickProvider**: Immediate response → AudioProviderCompleted (or directly video)
7. **VideoGeneratorService**: → VideoProviderRequested
8. **VideoFastProvider**: Immediate response → VideoGenerationResultPublished
9. **ContentService**: Receives result → Complete

### Retry Points to Test
- **Event Processing Failures**: 
  - What if TextNormalizer is down when event arrives?
  - What if VideoGenerator is down during audio operation?
  - Check ContentService inbox table for retry logic

- **Provider Failures**:
  - What if provider times out?
  - What if provider returns error?
  - Check for StepFailedEvent handling

- **State Inconsistencies**:
  - What if NormalizerResultPublished arrives but video request is already failed?
  - What if duplicate events arrive?
  - Check event idempotency (EventId in inbox)

---

## Database Consistency Checks

### PostgreSQL (ContentService)
**Table: customer_contents**
- ✓ Id: UUID primary key
- ✓ Url: Original source URL
- ✓ NormalizeStatus: NotStarted → InProgress → Completed/Failed
- ✓ NormalizeRequestId: Links to outline request
- ✓ VideoStatus: NotStarted → InProgress → Completed/Failed
- ✓ VideoRequestId: Links to video request
- ✓ AudioRequestId: Links to audio request (if applicable)
- ✓ OutlineProviderKey, AudioProviderKey, VideoProviderKey: Provider selection
- ✓ CreatedAtUtc, UpdatedAtUtc: Timestamps

**Consistency Rules to Validate**:
- VideoStatus should not be "Completed" if NormalizeStatus is "NotStarted"
- If OutlineProviderKey is null, NormalizeStatus should be "Skipped" or handled
- UpdatedAtUtc should advance with each operation

**Table: content_inbox_messages**
- ✓ EventId: Unique event identifier (idempotency key)
- ✓ EventName: Type of event
- ✓ Status: Unprocessed → Processed / Failed
- ✓ ProcessedAtUtc: When event was handled
- Should have retry mechanism for "Failed" status

### MongoDB (TextNormalizerService)
**Collection: outline_requests**
- ✓ _id: ObjectId
- ✓ CustomerId / ContentId: Reference to customer_contents
- ✓ ProviderKey: Which provider generated this
- ✓ Status: Pending / Completed / Failed
- ✓ ProviderTrackId: For polling providers
- ✓ CreatedAt: Timestamp

**Collection: outline_results**
- ✓ _id: ObjectId
- ✓ OutlineRequestId: Reference to outline_requests
- ✓ Script: Generated outline text
- ✓ CompletedAt: Timestamp

### MongoDB (VideoGeneratorService)
**Collection: audio_requests**
- ✓ _id: ObjectId
- ✓ ProviderKey: audio-quick or audio-hq
- ✓ Status: Pending / Completed / Failed
- ✓ ProviderTrackId: For audio-hq polling
- ✓ CreatedAt: Timestamp

**Collection: audio_results**
- ✓ _id: ObjectId
- ✓ AudioRequestId: Reference
- ✓ FileUrl: Generated audio file URL
- ✓ CompletedAt: Timestamp

**Collection: video_requests**
- ✓ _id: ObjectId
- ✓ ProviderKey: video-fast/sync/cloud/pro
- ✓ AudioUrls / AudioFilePaths: Input from audio phase
- ✓ Status: Pending / Completed / Failed
- ✓ ProviderTrackId: For polling providers
- ✓ CreatedAt: Timestamp

**Collection: video_results**
- ✓ _id: ObjectId
- ✓ VideoRequestId: Reference
- ✓ FileUrl: Generated video file URL
- ✓ CompletedAt: Timestamp

---

## Test Execution Checklist

For each scenario:
1. ✓ Send request to ContentService
2. ✓ Check ContentService database record created
3. ✓ Wait for all providers to complete
4. ✓ Verify final statuses in all databases
5. ✓ Check event inbox for proper processing
6. ✓ Validate state consistency across services
7. ✓ Test retry behavior by simulating failures

---

## Mock API Ports
- OutlineFast: 5040
- OutlineDetailed: 5041
- AudioQuick: 5050
- AudioHQ: 5051
- VideoFast: 5060
- VideoSync: 5063
- VideoCloud: 5062
- VideoPro: 5061

Core Services:
- ContentService: 5000
- TextNormalizerService: 5001
- VideoGeneratorService: 5002
