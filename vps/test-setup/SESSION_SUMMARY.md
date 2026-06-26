# Test Infrastructure - Session Summary

**Last Updated:** 2026-06-26  
**Commit:** 2dd90dbb (Feat: Complete test infrastructure with two-phase setup)  
**Branch:** feat/tester  
**Status:** ✅ All tests passing (6+6 scenarios complete)

---

## What Was Built

Complete test infrastructure for the microservice event flow with automated scenario testing and cleanup.

### Core Components

#### 1. **Two-Phase Setup Scripts** (`vps/test-setup/`)

**test-cleanup.sh** (Phases 1-5)
- Port inventory (3 microservices + 9 mock APIs)
- Database mapping (PostgreSQL + MongoDB)
- RabbitMQ connectivity check
- Database cleanup (deletes all test data)
- Port availability validation
- **Use:** Between test runs to clean old data and validate prerequisites

**test-start.sh** (Phases 6-7)
- Starts 9 Mock APIs
- Starts 3 Microservices
- Verifies all services RUNNING
- **Use:** After cleanup, before running scenarios

**test-scenario.sh** (Complete automation)
- Runs 6 customer-content scenarios
- Runs 6 analysis-content scenarios
- Auto-fetches successful CustomerContents for analysis
- Validates: `NormalizeStatus=COMPLETED AND VideoStatus=COMPLETED`

#### 2. **New Content Service Endpoint**

`CreateAnalysisContentFromScopeAsync` - Auto-fetch by ScopeKey
- Query: Latest N successful CustomerContents where `ScopeKey=X` and `NormalizeStatus=COMPLETED`
- Sort: By `CreationTime DESC`
- Result: Automatically creates AnalysisContent with fetched items
- Eliminates: Manual ID extraction in test scripts

**HTTP Endpoint:**
```
POST /api/content-service/v1/commercial/tests/analysis-contents-from-scope
{
  "ScopeKey": "scenario-001",
  "DomainName": "https://scenario.com",
  "Title": "Analysis for scenario-001",
  "MaxCustomerContents": 5
}
```

#### 3. **Redesigned HTTP Examples**

**examples-customer.http**
- Provider Registry table showing all 6 scenarios
- ScopeKey → Outline/Audio/Video provider mapping
- 6 scenarios covering all meaningful provider combinations
- External video (001-004) vs Internal video (005-006)

**examples-analysis.http**
- Same provider registry for consistency
- 6 scenarios using new `/analysis-contents-from-scope` endpoint
- Legacy fallback: Manual `/analysis-contents` endpoint documented
- Auto-fetch simplifies test execution

#### 4. **Documentation**

**TESTING.md** - Complete testing guide
- Two-phase workflow (cleanup → start → test)
- Success criteria per phase
- Quick reference commands
- Troubleshooting section

---

## Provider Registry (SubscriptionScopeRegistry)

All scenarios are pre-configured in the scope registry:

| ScopeKey     | Outline Provider  | Audio Provider | Video Provider       |
|--------------|-------------------|----------------|----------------------|
| scenario-001 | outline-fast      | audio-quick    | video-queue-external |
| scenario-002 | outline-fast      | audio-hq       | video-queue-external |
| scenario-003 | outline-queue     | audio-quick    | video-queue-external |
| scenario-004 | outline-queue     | audio-hq       | video-queue-external |
| scenario-005 | outline-fast      | [none]         | video-queue-internal |
| scenario-006 | outline-queue     | [none]         | video-queue-internal |

**Key Distinction:**
- External Video (001-004): Requires external audio input
- Internal Video (005-006): Creates audio internally, no external audio needed

---

## Service Ports

**Microservices:**
- 7450: Content Service
- 7460: Text-Normalizer Service  
- 7470: Video-Generator Service

**Mock APIs:**
- 5040: OutlineFast
- 5041: OutlineQueue
- 5050: AudioQuick
- 5051: AudioHQ
- 5061: VideoQueueInternal
- 5062: VideoQueueExternal
- 5070: CdnLocalMinio
- 5071: CdnBunnySelf
- 5072: CdnBunnyS3

**Infrastructure:**
- 5432: PostgreSQL (HHS_ContentService_Dev)
- 27017: MongoDB Text-Normalizer
- 27017: MongoDB Video-Generator
- 5672: RabbitMQ

---

## Test Workflow (Day-to-Day)

### Initial Setup
```bash
vps/test-setup/test-cleanup.sh   # Clean databases, validate ports
vps/test-setup/test-start.sh     # Start all services
vps/test-setup/test-scenario.sh  # Run all 12 scenarios (6+6)
```

### Between Test Runs
```bash
vps/test-setup/test-cleanup.sh      # Clean old data
pkill -9 dotnet                     # (optional) kill services
vps/test-setup/test-start.sh        # Restart services
vps/test-setup/test-scenario.sh     # Run tests again
```

### After Testing
```bash
pkill -9 dotnet                     # Stop all services
```

---

## Test Scenarios Explained

### Customer Content (6 scenarios)

**001: Fast Outline + Quick Audio + External Video**
- Providers: outline-fast (5s) → audio-quick (3s) → video-queue-external (30s polling)
- Expected: ~38 seconds

**002: Fast Outline + HQ Audio + External Video**
- Providers: outline-fast (5s) → audio-hq (20s polling) → video-queue-external (30s polling)
- Expected: ~53 seconds

**003: Queue Outline + Quick Audio + External Video**
- Providers: outline-queue (10s polling) → audio-quick (3s) → video-queue-external (30s polling)
- Expected: ~43 seconds

**004: Queue Outline + HQ Audio + External Video**
- Providers: outline-queue (10s polling) → audio-hq (20s polling) → video-queue-external (30s polling)
- Expected: ~58 seconds (maximum complexity)

**005: Fast Outline + Internal Video**
- Providers: outline-fast (5s) → video-queue-internal (30s polling, creates own audio)
- Expected: ~35 seconds

**006: Queue Outline + Internal Video**
- Providers: outline-queue (10s polling) → video-queue-internal (30s polling, creates own audio)
- Expected: ~40 seconds

### Analysis Content (same 6 scenarios)

- Uses new `/analysis-contents-from-scope` endpoint
- Auto-fetches latest 5 successful CustomerContents by ScopeKey
- Processes all items through same provider pipeline
- Final video is composite/merged from all items
- Same expected durations as customer-content equivalents

---

## Success Criteria

**Per Scenario:**
```sql
SELECT "NormalizeStatus", "VideoStatus" FROM "CustomerContents" WHERE "Id" = ?
-- Result: NormalizeStatus=COMPLETED AND VideoStatus=COMPLETED
```

**Full Test Suite:**
- Customer Content: 6/6 COMPLETED ✅
- Analysis Content: 6/6 COMPLETED ✅

---

## Database Cleanup

All test data is deleted by `test-cleanup.sh`:

**PostgreSQL (HHS_ContentService_Dev):**
- CustomerContents
- AnalysisContents
- AnalysisContentItems
- EventInboxMessages
- CustomerVpSettings
- ContentVideoGenerationLimits

**MongoDB Text-Normalizer (HHS_TextNormalizerService_Dev):**
- CustomerContentNormalizedRequests
- AnalysisContentNormalizedRequests
- EventInboxMessages

**MongoDB Video-Generator (HHS_VideoGeneratorService_Dev):**
- VideoRequests
- AudioRequests
- EventInboxMessages

---

## Modified Files This Session

1. **ContentOperationAppService.cs**
   - Added `CreateAnalysisContentFromScopeRequest` DTO
   - Added `CreateAnalysisContentFromScopeAsync()` method
   - Added `using HsnSoft.Base.Domain.Models` for ListQueryOptions

2. **TestController.cs**
   - Added `/analysis-contents-from-scope` endpoint
   - Disables multi-tenant & scope subscription filters

3. **examples-customer.http**
   - Complete redesign with provider registry table
   - 6 scenarios with provider chains and expected durations
   - Updated to new endpoint URL path

4. **examples-analysis.http**
   - Complete redesign with provider registry table
   - 6 scenarios using `/analysis-contents-from-scope`
   - Legacy manual endpoint documented for reference

5. **New test infrastructure files:**
   - test-cleanup.sh
   - test-start.sh
   - test-scenario.sh
   - TESTING.md

---

## Next Session TODOs

- [ ] Test end-to-end with fresh database
- [ ] Verify all service logs are clean (no unexpected errors)
- [ ] Document any issues found during testing
- [ ] Consider additional edge case scenarios if needed

---

## Debugging Tips

**When tests fail or timeout:**

1. Check EventInboxMessages status in each service:
   ```bash
   # Content Service
   PGPASSWORD=postgres psql -h localhost -U postgres -d HHS_ContentService_Dev \
     -c "SELECT \"Status\", COUNT(*) FROM \"EventInboxMessages\" GROUP BY \"Status\";"
   ```

2. View service logs:
   ```bash
   tail -100 /tmp/service_logs/text-normalizer.log
   tail -100 /tmp/service_logs/video-generator.log
   tail -100 /tmp/service_logs/content.log
   ```

3. Check specific content status:
   ```bash
   PGPASSWORD=postgres psql -h localhost -U postgres -d HHS_ContentService_Dev \
     -c "SELECT \"ScopeKey\", \"NormalizeStatus\", \"VideoStatus\", \"LastError\" FROM \"CustomerContents\" WHERE \"Id\" = '...';"
   ```

---

## Key Learnings

- ✅ Two-phase setup (cleanup → start) is cleaner than monolithic
- ✅ Auto-fetch endpoint simplifies test script maintenance
- ✅ Provider registry table documentation prevents scenario confusion
- ✅ Test timeout should be 180s per scenario, not 5 minutes
- ✅ All 12 scenarios (6+6) now test successfully in ~10 minutes total
