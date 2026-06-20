# CDN Settings Hierarchy

## Overview

CDN configuration uses a hierarchical structure similar to video-provider/video-fast-internal pattern:

```
ProviderSettingsBase (generic provider base)
    ↓
CdnProviderSettingsBase (CDN-specific base)
    ↓
BrandSpecificSettings (CloudflareCdnSettings, BunnyCdnSettings, etc.)
```

## Base Settings Structure

### `CdnProviderSettingsBase` (Hhs.Shared/Configuration)

**Common to ALL CDN providers:**

| Field | Type | Required | Default | Purpose |
|-------|------|----------|---------|---------|
| `BaseUrl` | string | ✅ | - | Public CDN endpoint URL |
| `ApiKey` | string | ✅ | - | Primary authentication credential |
| `ApiSecret` | string? | ⚠️ | null | Secondary credential (S3-style auth) |
| `ZoneName` | string | ✅ | - | Bucket/Zone identifier in CDN |
| `UploadPathFolder` | string | ✅ | /videos | Base path for file uploads |
| `ZonePath` | string | ✅ | media | URL path component (public access) |
| `PathPrefix` | string | ✅ | prod | URL path prefix (environment) |
| `Storage` | StorageProviderSettingsBase | ✅ | - | Backend storage configuration |

### Storage Configuration

```json
"Storage": {
  "Type": "enum",     // HttpCdn, S3, AzureBlob, LocalMinio, etc.
  "Url": "string"     // Backend storage endpoint (e.g., S3, MinIO)
}
```

## Brand-Specific Settings

### 1. LocalMinio (`LocalMinioCdnSettings`)

**Common Fields:**
```json
{
  "BaseUrl": "http://localhost:5070",
  "ApiKey": "minioadmin",
  "ZoneName": "cdn-local-minio-bucket",
  "UploadPathFolder": "/videos"
}
```

**Minio-Specific Fields:**
```json
{
  "MinioAccessKey": "minioadmin",
  "MinioSecretKey": "minioadmin",
  "MinioBucket": "cdn-local-minio-bucket"
}
```

**Full Example:**
```json
"CdnLocalMinio": {
  "BaseUrl": "http://localhost:5070",
  "ApiKey": "minioadmin",
  "ApiSecret": "minioadmin",
  "ZoneName": "cdn-local-minio-bucket",
  "UploadPathFolder": "/videos",
  "ZonePath": "media",
  "PathPrefix": "minio",
  "MinioAccessKey": "minioadmin",
  "MinioSecretKey": "minioadmin",
  "MinioBucket": "cdn-local-minio-bucket",
  "Storage": {
    "Type": "HttpCdn",
    "Url": "http://localhost:5070"
  }
}
```

---

### 2. Bunny CDN - Self-Hosted (`BunnyCdnSettings`)

**Common Fields:**
```json
{
  "BaseUrl": "https://cdn.example.com",
  "ApiKey": "bunny_api_key",
  "ZoneName": "my-bunny-zone",
  "UploadPathFolder": "/hhs/videos"
}
```

**Bunny-Specific Fields:**
```json
{
  "AccountId": "bunny_account_id",
  "StorageRegion": "us-west",
  "AccessKey": "bunny_storage_access_key"
}
```

**Full Example:**
```json
"CdnBunnySelf": {
  "BaseUrl": "https://pull.bunny.net",
  "ApiKey": "bunny_api_token",
  "ApiSecret": null,
  "ZoneName": "my-bunny-zone",
  "UploadPathFolder": "/hhs/videos",
  "ZonePath": "media",
  "PathPrefix": "bunny",
  "AccountId": "123456",
  "StorageRegion": "us-west",
  "AccessKey": "bunny_storage_key",
  "Storage": {
    "Type": "HttpCdn",
    "Url": "https://my-bunny-zone.b-cdn.net"
  }
}
```

---

### 3. Bunny CDN - S3 Backend (`BunnyCdnSettings`)

**Bunny with S3-compatible storage:**

```json
"CdnBunnyS3": {
  "BaseUrl": "https://pull.bunny.net",
  "ApiKey": "bunny_api_token",
  "ApiSecret": "bunny_s3_secret",
  "ZoneName": "bunny-s3-zone",
  "UploadPathFolder": "/hhs/videos",
  "ZonePath": "videos",
  "PathPrefix": "s3",
  "AccountId": "123456",
  "StorageRegion": "eu-central-1",
  "AccessKey": "bunny_s3_access_key",
  "Storage": {
    "Type": "S3",
    "Url": "https://s3.eu-central-1.backblazeb2.com"
  }
}
```

---

### 4. Cloudflare R2 (`CloudflareCdnSettings`)

**Cloudflare-Specific Fields:**
```json
{
  "ZoneId": "cloudflare_zone_id",
  "AccountId": "cloudflare_account_id",
  "NamespaceId": "r2_namespace_id"
}
```

**Full Example:**
```json
"CdnCloudflare": {
  "BaseUrl": "https://cdn.example.com",
  "ApiKey": "cloudflare_api_token",
  "ApiSecret": null,
  "ZoneName": "my-cloudflare-bucket",
  "UploadPathFolder": "/hhs/videos",
  "ZonePath": "media",
  "PathPrefix": "cf",
  "ZoneId": "zone123",
  "AccountId": "account456",
  "NamespaceId": "namespace789",
  "Storage": {
    "Type": "S3",
    "Url": "https://s3.eu.cloudflare.com"
  }
}
```

---

### 5. AWS CloudFront (`AwsCloudFrontCdnSettings`)

**AWS-Specific Fields:**
```json
{
  "DistributionId": "E1234ABCD"
}
```

**Full Example:**
```json
"CdnAwsCloudFront": {
  "BaseUrl": "https://d123456.cloudfront.net",
  "ApiKey": "aws_access_key",
  "ApiSecret": "aws_secret_key",
  "ZoneName": "my-s3-bucket",
  "UploadPathFolder": "/hhs/videos",
  "ZonePath": "media",
  "PathPrefix": "aws",
  "DistributionId": "E1234ABCD",
  "Storage": {
    "Type": "S3",
    "Url": "https://s3.us-east-1.amazonaws.com"
  }
}
```

---

### 6. Azure CDN (`AzureCdnSettings`)

**Azure-Specific Fields:**
```json
{
  "ProfileName": "my-azure-cdn-profile"
}
```

**Full Example:**
```json
"CdnAzure": {
  "BaseUrl": "https://mycdn.azureedge.net",
  "ApiKey": "azure_api_key",
  "ApiSecret": null,
  "ZoneName": "my-storage-account",
  "UploadPathFolder": "/hhs/videos",
  "ZonePath": "media",
  "PathPrefix": "azure",
  "ProfileName": "my-cdn-profile",
  "Storage": {
    "Type": "AzureBlob",
    "Url": "https://mystorageaccount.blob.core.windows.net"
  }
}
```

---

## Folder Structure in MinIO/S3

All uploads follow this pattern:

```
Bucket: cdn-local-minio-bucket (or any zone name)
├── videos/                              (UploadPathFolder)
│   └── 2026/06/20/                     (Date-based)
│       ├── 3fbd4cbd_video.mp4
│       ├── 665b3f8f_test.txt
│       └── a1b2c3d4_presentation.pdf
```

Or with environment prefix:
```
Bucket: cdn-local-minio-bucket
├── prod/videos/2026/06/20/...         (PathPrefix + UploadPathFolder)
├── staging/videos/2026/06/20/...
└── dev/videos/2026/06/20/...
```

---

## Configuration Hierarchy Summary

```
appsettings.json
├── Provider
│   └── Cdn
│       ├── CdnLocalMinio ────────→ LocalMinioCdnSettings
│       ├── CdnBunnySelf ─────────→ BunnyCdnSettings
│       ├── CdnBunnyS3 ──────────→ BunnyCdnSettings
│       ├── CdnCloudflare ───────→ CloudflareCdnSettings
│       ├── CdnAwsCloudFront ───→ AwsCloudFrontCdnSettings
│       └── CdnAzure ───────────→ AzureCdnSettings
```

Each extends:
```
CdnProviderSettingsBase
├── BaseUrl
├── ApiKey
├── ApiSecret
├── ZoneName
├── UploadPathFolder
├── ZonePath
├── PathPrefix
├── Storage
└── Brand-Specific Fields (if any)
```

---

## Adding a New CDN Provider

### Step 1: Create Settings Class

**File:** `Hhs.Shared/Configuration/YourCdnSettings.cs`

```csharp
namespace Hhs.Shared.Configuration;

public sealed class YourCdnSettings : CdnProviderSettingsBase
{
    // Add brand-specific fields
    public string? CustomField1 { get; set; }
    public string? CustomField2 { get; set; }
}
```

### Step 2: Add to appsettings.json

```json
"Provider": {
  "Cdn": {
    "CdnYourBrand": {
      "BaseUrl": "https://your-cdn.example.com",
      "ApiKey": "your_api_key",
      "ApiSecret": "your_api_secret",
      "ZoneName": "your-zone-name",
      "UploadPathFolder": "/your/path",
      "ZonePath": "media",
      "PathPrefix": "prod",
      "CustomField1": "value1",
      "CustomField2": "value2",
      "Storage": {
        "Type": "S3",
        "Url": "https://your-storage.example.com"
      }
    }
  }
}
```

### Step 3: Register in Program.cs

```csharp
builder.Services.Configure<YourCdnSettings>(
    builder.Configuration.GetSection("Provider:Cdn:CdnYourBrand"));
```

---

## Testing CDN Configuration

Use `Hhs.MockApi.Tester` to verify:

```bash
cd Hhs.MockApi.Tester
echo "1" | dotnet run    # Test CdnLocalMinio
echo "2" | dotnet run    # Test all CDNs
```

Expected output for each CDN:
- ✅ Upload successful
- ✅ Download successful
- ✅ Hash verification PASSED
- ✅ File storage verified
