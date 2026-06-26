# Complete Testing Guide

## Overview

This directory contains the complete test setup and execution pipeline:

| Script | Purpose |
|--------|---------|
| `test-cleanup.sh` | Cleanup & validation (Phases 1-5) |
| `test-start.sh` | Start services (Phases 6-7) |
| `test-scenario.sh` | Execute all test scenarios |
| `TESTING.md` | This documentation |

---

## Test Success Definition

A scenario is **COMPLETE** when database shows:
```
NormalizeStatus = COMPLETED
VideoStatus = COMPLETED
```

Both fields must be COMPLETED. Anything else = test still running or failed.

---

## Two-Phase Testing Workflow

### Phase 1: Cleanup & Validation (Optional between test runs)

```bash
chmod +x vps/test-setup/test-cleanup.sh
vps/test-setup/test-cleanup.sh
```

**What happens (5 phases):**
1. **Ports Inventory** - Lists all 12 ports (3 microservices + 9 mock APIs)
2. **Database Mapping** - Shows PostgreSQL tables + MongoDB collections  
3. **RabbitMQ Check** - Verifies RabbitMQ connection
4. **Database Cleanup** - Deletes ALL test data from all tables/collections
5. **Port Availability** - Confirms all 12 ports are free (or shows which are in use)

**Use this when:**
- Starting fresh for a new test run
- Tests finished and you want to clean old data before next run
- You want to stop services (run `pkill -9 dotnet` after cleanup completes)

**Success indicators:**
```
✓ Ports: All free ✓
✓ Databases: Cleaned ✓
✓ RabbitMQ: Connected ✓
✓ CLEANUP COMPLETE - READY FOR SERVICE START
```

### Phase 2: Start Services

```bash
chmod +x vps/test-setup/test-start.sh
vps/test-setup/test-start.sh
```

**What happens (2 phases):**
6. **Service Startup** - Starts 3 microservices + 9 mock APIs
7. **Verification** - Confirms all services are RUNNING & RESPONSIVE

**Success indicators:**
```
✓ All services ready!
✓ Microservices (3/3): content, text-normalizer, video-generator - RUNNING
✓ Mock APIs (9/9): All running
✓ SERVICES RUNNING - READY FOR TESTING
```

---

### Phase 3: Run Test Scenarios

**Only after both cleanup & start phases complete successfully.**

```bash
chmod +x vps/test-setup/test-scenario.sh
vps/test-setup/test-scenario.sh
```

**What happens:**

#### Phase 1: Customer Content Scenarios (001-006)
- Scenario-001: Fast outline + Quick audio + External video (expected: ~38s)
- Scenario-002: Fast outline + HQ audio + External video (expected: ~53s)
- Scenario-003: Queue outline + Quick audio + External video (expected: ~43s)
- Scenario-004: Queue outline + HQ audio + External video (expected: ~58s)
- Scenario-005: Fast outline + Internal video (expected: ~35s)
- Scenario-006: Queue outline + Internal video (expected: ~40s)

For each:
1. POST request to `/customer-contents` with scenarioN details
2. Get ContentId from response
3. Poll database every 2 seconds (max 180s timeout)
4. Check: `NormalizeStatus=COMPLETED AND VideoStatus=COMPLETED`
5. Display: elapsed time + status updates

**Success output:**
```
[Scenario] scenario-001: Creating...
  ID: [UUID]
  ✓ [42s] COMPLETED

[Scenario] scenario-002: Creating...
  ID: [UUID]
  ✓ [55s] COMPLETED
  
... (scenarios 003-006)

Customer Content Results: ✓ 6 passed | ✗ 0 failed
```

#### Phase 2: Analysis Content Scenarios (001-006)
- Uses actual CustomerContentIds from Phase 1
- Same provider combinations as customer-content
- Creates multi-item analysis from successful customer content IDs
- Same validation: `NormalizeStatus=COMPLETED AND VideoStatus=COMPLETED`

**Success output:**
```
[Scenario] scenario-001: Creating analysis...
  ID: [UUID]
  ✓ [45s] COMPLETED

... (scenarios 002-006)

Analysis Content Results: ✓ 6 passed | ✗ 0 failed

╔════════════════════════════════════════════════════════╗
║    TEST EXECUTION SUMMARY                             ║
╚════════════════════════════════════════════════════════╝

Customer Content: ✓ 6/6 passed
Analysis Content: ✓ 6/6 passed

✓ ALL TESTS PASSED
```

---

## Test Scenarios Explained

### Customer Content (001-006)

**GROUP 1: External Video (requires audio)**

| Scenario | Outline | Audio | Video | Duration |
|----------|---------|-------|-------|----------|
| 001 | Fast (5s) | Quick (3s) | External | ~38s |
| 002 | Fast (5s) | HQ+Poll (20s) | External | ~53s |
| 003 | Poll (10s) | Quick (3s) | External | ~43s |
| 004 | Poll (10s) | HQ+Poll (20s) | External | ~58s |

**GROUP 2: Internal Video (creates own audio)**

| Scenario | Outline | Video | Duration |
|----------|---------|-------|----------|
| 005 | Fast (5s) | Internal | ~35s |
| 006 | Poll (10s) | Internal | ~40s |

### Analysis Content (001-006)

Same 6 provider combinations as customer-content, but:
- Processes multiple customer content items together
- Uses actual ContentIds from successful customer-content scenarios
- Final video is composite/merged from all items

---

## Databases & Cleanup

### PostgreSQL (HHS_ContentService_Dev)

**Tables (cleaned before each test):**
- CustomerContents
- AnalysisContents
- AnalysisContentItems
- EventInboxMessages
- CustomerVpSettings
- ContentVideoGenerationLimits

**Success columns:**
- `NormalizeStatus` (expected: COMPLETED)
- `VideoStatus` (expected: COMPLETED)

**Query to check results:**
```sql
SELECT 
  "Id",
  "ScopeKey",
  "NormalizeStatus",
  "VideoStatus",
  "CreationTime"
FROM "CustomerContents"
WHERE "DomainName" = 'https://scenario.com'
ORDER BY "CreationTime" DESC;
```

### MongoDB - Text-Normalizer (HHS_TextNormalizerService_Dev)

**Collections (cleaned):**
- CustomerContentNormalizedRequests
- AnalysisContentNormalizedRequests
- EventInboxMessages

### MongoDB - Video-Generator (HHS_VideoGeneratorService_Dev)

**Collections (cleaned):**
- VideoRequests
- AudioRequests
- EventInboxMessages

---

## Troubleshooting

### Setup Fails: "Port already in use"

**Issue:** Port X (service Y) already listening

**Fix:**
```bash
pkill -9 dotnet
sleep 2
vps/test-setup/test-setup.sh
```

### Setup Fails: "Database connection failed"

**Check PostgreSQL:**
```bash
psql -h localhost -U postgres -d HHS_ContentService_Dev -c "SELECT 1"
```

**Check MongoDB:**
```bash
mongosh mongodb://localhost:27017 --eval "db.adminCommand('ping')"
```

### Test Scenario Timeout (180s)

**Likely issues:**
1. Setup didn't complete all 7 phases
2. Services crashed after startup (check `/tmp/service_logs/*.log`)
3. Mock APIs not responding (check ports 5040, 5050, 5070, 5062)

**Debug:**
```bash
# Check service logs
tail -50 /tmp/service_logs/text-normalizer.log
tail -50 /tmp/service_logs/video-generator.log

# Manually check database
PGPASSWORD=postgres psql -h localhost -U postgres -d HHS_ContentService_Dev -c \
  "SELECT \"ScopeKey\", \"NormalizeStatus\", \"VideoStatus\" FROM \"CustomerContents\" LIMIT 5;"
```

---

## Configuration

### Ports (Fixed)

**Microservices:**
- Content Service: 7450
- Text-Normalizer: 7460
- Video-Generator: 7470

**Mock APIs (9 required for full testing):**
- OutlineFast: 5040
- OutlineQueue: 5041
- AudioQuick: 5050
- AudioHQ: 5051
- VideoQueueInternal: 5061
- VideoQueueExternal: 5062
- CdnLocalMinio: 5070
- CdnBunnySelf: 5071
- CdnBunnyS3: 5072

**Infrastructure:**
- PostgreSQL: 5432
- MongoDB: 27017
- RabbitMQ: 5672

### Timeouts

- Per-scenario timeout: 180 seconds
- Poll interval: 2 seconds
- Service startup timeout: 60 seconds

---

## Quick Reference

```bash
# INITIAL SETUP (one time)
vps/test-setup/test-cleanup.sh
vps/test-setup/test-start.sh
vps/test-setup/test-scenario.sh

# BETWEEN TEST RUNS (cleanup old data)
vps/test-setup/test-cleanup.sh      # Clean databases & check ports
vps/test-setup/test-start.sh        # Restart services
vps/test-setup/test-scenario.sh     # Run tests

# STOP SERVICES (after testing)
pkill -9 dotnet

# Check test results
PGPASSWORD=postgres psql -h localhost -U postgres -d HHS_ContentService_Dev -c \
  "SELECT \"ScopeKey\", \"NormalizeStatus\", \"VideoStatus\" FROM \"CustomerContents\" 
   WHERE \"DomainName\" = 'https://scenario.com' 
   ORDER BY \"CreationTime\" DESC LIMIT 10;"

# View logs
ls -lh /tmp/service_logs/
tail -f /tmp/service_logs/video-generator.log
```

---

## Success Criteria

✓ test-cleanup.sh completes all 5 phases
✓ test-start.sh completes all 2 phases  
✓ All 12 services running (verified by test-start.sh)
✓ Database cleaned (all old data removed)
✓ Customer Content scenarios 001-006: All COMPLETED (6/6)
✓ Analysis Content scenarios 001-006: All COMPLETED (6/6)
✓ Final output: "✓ ALL TESTS PASSED"
