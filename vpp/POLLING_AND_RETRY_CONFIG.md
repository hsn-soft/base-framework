# Polling and Retry Configuration

## Overview

Provider polling and event retry behavior can now be configured via `appsettings.json` instead of hardcoded values.

## Configuration Options

Add these sections to your `appsettings.json`:

```json
{
  "Polling": {
    "OutlinePollingIntervalSeconds": 5,
    "AudioPollingIntervalSeconds": 5,
    "VideoPollingIntervalSeconds": 5,
    "MaxOutlinePollingAttempts": 10,
    "MaxAudioPollingAttempts": 10,
    "MaxVideoPollingAttempts": 10
  },
  "Retry": {
    "DelaySeconds": [60, 120, 300, 900],
    "MaxRetryCount": 4
  },
  "Timeout": {
    "OutlineProviderTimeoutSeconds": 30,
    "AudioProviderTimeoutSeconds": 30,
    "VideoProviderTimeoutSeconds": 60
  }
}
```

## Configuration Details

### Polling Section
Controls how frequently providers are polled and how many attempts are allowed.

**OutlinePollingIntervalSeconds** (default: 5)
- How many seconds to wait before re-polling an outline provider
- Applied in: SaveOutlinePollingStateAsync, OutlineProviderPollingAppService

**MaxOutlinePollingAttempts** (default: 10)
- Maximum number of polling attempts before failing the request
- If provider doesn't respond after this many attempts, status changes to FAILED

### Retry Section
Controls how failed events are retried.

**DelaySeconds** (default: [60, 120, 300, 900])
- Array of retry delays in seconds
- Format: retry_attempt_1, retry_attempt_2, retry_attempt_3, ...
- Example: [60, 120, 300, 900] means:
  - 1st retry: wait 60 seconds (1 minute)
  - 2nd retry: wait 120 seconds (2 minutes)
  - 3rd retry: wait 300 seconds (5 minutes)
  - 4th+ retries: wait 900 seconds (15 minutes)

**MaxRetryCount** (default: 4)
- Maximum number of retry attempts before permanently failing

### Timeout Section
Controls HTTP request timeouts for provider calls.

## Flow Example

Assuming config:
- OutlinePollingIntervalSeconds: 5
- MaxOutlinePollingAttempts: 3

**Timeline:**
1. Request sent to provider at T=0
2. Provider says "processing" → schedule next poll at T+5s
3. At T=5s, check again → still processing → schedule next poll at T+10s
4. At T=10s, check again → still processing → schedule next poll at T+15s
5. At T=15s, check again → reached MaxOutlinePollingAttempts (3 polls)
6. Change status to FAILED, publish StepFailedEto event

## Where Configuration is Used

### NormalizerOperationAppService
- Line ~61: Sets `MaxOutlinePollingCount` from `PollingOptions.MaxOutlinePollingAttempts`
- Line ~259: Sets initial `NextOutlinePollAtUtc` with `OutlinePollingIntervalSeconds`

### OutlineProviderPollingAppService
- Line ~98: Sets `NextOutlinePollAtUtc` with `OutlinePollingIntervalSeconds` for retries

### RetryDelayCalculator
- Uses `DelaySeconds` array to calculate wait time between event retries
- Injected in Program.cs based on RetryOptions from configuration

## Implementation Classes

- `PollingOptions`: Located in `Hhs.Shared/Configuration/PollingOptions.cs`
- `RetryOptions`: Located in `Hhs.Shared/Configuration/PollingOptions.cs`
- `TimeoutOptions`: Located in `Hhs.Shared/Configuration/PollingOptions.cs`
- `RetryDelayCalculator`: Located in `Hhs.Shared/Retry/RetryDelayCalculator.cs`

## Service Registration

In `Program.cs`:

```csharp
var pollingOptions = builder.Configuration
    .GetSection(PollingOptions.SectionName)
    .Get<PollingOptions>() ?? new PollingOptions();
builder.Services.AddSingleton(pollingOptions);

var retryOptions = builder.Configuration
    .GetSection(RetryOptions.SectionName)
    .Get<RetryOptions>() ?? new RetryOptions();
builder.Services.AddSingleton(_ => new RetryDelayCalculator(retryOptions.DelaySeconds));
```

## Future Work

Remaining hardcoded values to be externalized:
- NormalizerRetryAppService (line 60): AddSeconds(10) retry delay
- Audio/Video polling similar to outline polling
- Audio/Video retry services
