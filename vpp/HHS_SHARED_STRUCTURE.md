# Hhs.Shared Architecture - Reorganized Structure

## Overview

The `Hhs.Shared` library is now organized into two main sections with clear separation of concerns:

- **Configuration/**: Settings, configuration classes, and configuration models
- **Providers/**: Provider implementations, factories, and resolvers

## Complete Folder Structure

```
Hhs.Shared/
│
├── Providers/
│   ├── ProviderKeys.cs           (Provider identifier constants)
│   ├── ProviderModels.cs         (Provider model definitions)
│   │
│   └── Storage/
│       ├── ICdnStorageProvider.cs              (interface)
│       ├── CdnStorageProviderFactory.cs        (factory)
│       ├── CdnProviderResolver.cs              (resolver)
│       │
│       ├── S3StorageProvider.cs                (AWS S3 implementation)
│       ├── LocalStorageProvider.cs             (Local disk implementation)
│       ├── AzureStorageProvider.cs             (Azure Blob implementation)
│       ├── CloudflareR2StorageProvider.cs      (Cloudflare R2 implementation)
│       └── HttpCdnStorageProvider.cs           (Generic HTTP implementation)
│
└── Configuration/
    ├── ProviderSettingsBase.cs     (Generic provider settings base)
    ├── RetrySettingsBase.cs         (Retry policy configuration)
    ├── PollingSettingsBase.cs       (Polling behavior configuration)
    │
    └── Providers/
        ├── Outline/
        │   └── OutlineProviderSettings.cs
        │
        ├── Audio/
        │   └── AudioProviderSettings.cs
        │
        ├── Video/
        │   └── VideoProviderSettings.cs
        │
        └── Storage/
            ├── StorageProviderSettingsBase.cs       (Base for all storage settings)
            ├── LocalStorageSettings.cs              (Local disk config)
            ├── S3StorageSettings.cs                 (AWS S3 config)
            ├── AzureBlobStorageSettings.cs          (Azure Blob config)
            ├── CloudflareR2StorageSettings.cs       (Cloudflare R2 config)
            │
            ├── CdnProviderSettingsBase.cs           (Base for all CDN settings)
            ├── LocalMinioCdnSettings.cs             (LocalMinio CDN config)
            ├── BunnyCdnSettings.cs                  (Bunny CDN config)
            ├── CloudflareCdnSettings.cs             (Cloudflare CDN config)
            ├── AzureCdnSettings.cs                  (Azure CDN config)
            └── AwsCloudFrontCdnSettings.cs          (AWS CloudFront config)
```

## Namespace Organization

### Providers Namespaces

```
Hhs.Shared.Providers                    ← ProviderKeys, ProviderModels
Hhs.Shared.Providers.Storage            ← All storage provider implementations
```

### Configuration Namespaces

```
Hhs.Shared.Configuration                ← Base settings classes
Hhs.Shared.Configuration.Providers.Outline    ← Outline-specific settings
Hhs.Shared.Configuration.Providers.Audio      ← Audio-specific settings
Hhs.Shared.Configuration.Providers.Video      ← Video-specific settings
Hhs.Shared.Configuration.Providers.Storage    ← Storage & CDN settings
```

## File Organization by Purpose

### 1. Provider Implementations (Hhs.Shared/Providers/Storage/)

These classes implement the actual provider logic:

| File | Purpose |
|------|---------|
| `ICdnStorageProvider.cs` | Interface for all CDN storage providers |
| `S3StorageProvider.cs` | S3-compatible storage (AWS S3, MinIO) |
| `LocalStorageProvider.cs` | Local filesystem storage |
| `AzureStorageProvider.cs` | Azure Blob Storage |
| `CloudflareR2StorageProvider.cs` | Cloudflare R2 object storage |
| `HttpCdnStorageProvider.cs` | Generic HTTP-based CDN |

### 2. Provider Management (Hhs.Shared/Providers/Storage/)

Factory and resolver patterns for provider creation:

| File | Purpose |
|------|---------|
| `CdnStorageProviderFactory.cs` | Creates provider instances by type |
| `CdnProviderResolver.cs` | Resolves provider settings by key |

### 3. Core Provider Identifiers (Hhs.Shared/Providers/)

Global provider definitions:

| File | Purpose |
|------|---------|
| `ProviderKeys.cs` | Provider identifier constants |
| `ProviderModels.cs` | Provider model definitions |

### 4. Configuration Settings (Hhs.Shared/Configuration/Providers/Storage/)

Configuration classes for all storage and CDN providers:

#### Storage Settings
| File | Purpose |
|------|---------|
| `StorageProviderSettingsBase.cs` | Base class for all storage settings |
| `LocalStorageSettings.cs` | Local filesystem configuration |
| `S3StorageSettings.cs` | S3-compatible storage configuration |
| `AzureBlobStorageSettings.cs` | Azure Blob Storage configuration |
| `CloudflareR2StorageSettings.cs` | Cloudflare R2 configuration |

#### CDN Settings
| File | Purpose |
|------|---------|
| `CdnProviderSettingsBase.cs` | Base class for all CDN settings |
| `LocalMinioCdnSettings.cs` | LocalMinio CDN configuration |
| `BunnyCdnSettings.cs` | Bunny CDN configuration |
| `CloudflareCdnSettings.cs` | Cloudflare CDN configuration |
| `AzureCdnSettings.cs` | Azure CDN configuration |
| `AwsCloudFrontCdnSettings.cs` | AWS CloudFront configuration |

### 5. Other Settings (Hhs.Shared/Configuration/)

Generic and shared configuration classes:

| File | Purpose |
|------|---------|
| `ProviderSettingsBase.cs` | Generic provider settings base |
| `RetrySettingsBase.cs` | Retry policy configuration |
| `PollingSettingsBase.cs` | Polling behavior configuration |

## Design Principles

### 1. **Separation of Concerns**
- **Configuration/**: Defines WHAT (structure and constraints)
- **Providers/**: Implements HOW (logic and behavior)

### 2. **Hierarchical Organization**
- Generic types at root level
- Specific implementations in subfolders
- Clear "from generic to specific" pattern

### 3. **Type-Based Grouping**
- Storage providers grouped under `Providers/Storage/`
- Storage settings grouped under `Configuration/Providers/Storage/`
- Outline/Audio/Video settings in their respective folders

### 4. **Naming Consistency**
- Settings: `*Settings.cs` (e.g., `LocalStorageSettings.cs`)
- Providers: `*Provider.cs` (e.g., `LocalStorageProvider.cs`)
- Base classes: `*SettingsBase.cs`, `*Base.cs`
- Interfaces: `I*` (e.g., `ICdnStorageProvider.cs`)

## Usage Examples

### Adding a New CDN Provider

1. **Create settings class** in `Configuration/Providers/Storage/`:
   ```csharp
   // MyNewCdnSettings.cs
   namespace Hhs.Shared.Configuration.Providers.Storage;
   
   public sealed class MyNewCdnSettings : CdnProviderSettingsBase
   {
       public string? CustomField { get; set; }
   }
   ```

2. **Create provider implementation** in `Providers/Storage/`:
   ```csharp
   // MyNewCdnProvider.cs
   namespace Hhs.Shared.Providers.Storage;
   
   public sealed class MyNewCdnProvider : ICdnStorageProvider
   {
       public async Task<(string StorageUrl, string CdnUrl)> UploadAsync(
           Stream fileStream, 
           string filename, 
           CancellationToken cancellationToken)
       {
           // Implementation
       }
   }
   ```

3. **Register in factory** (`CdnStorageProviderFactory.cs`):
   ```csharp
   "mynewcdn" => new MyNewCdnProvider(
       cdnSettings,
       storageSettings as MyNewCdnSettings ?? ...,
       _httpClient,
       _loggerFactory.CreateLogger<MyNewCdnProvider>()),
   ```

4. **Add to appsettings.json**:
   ```json
   "Provider": {
     "Cdn": {
       "MyNewCdn": {
         "BaseUrl": "...",
         "ApiKey": "...",
         "CustomField": "..."
       }
     }
   }
   ```

### Adding a New Provider Type (e.g., Caching)

1. Create `Providers/Caching/` subdirectory
2. Create settings in `Configuration/Providers/Caching/`
3. Follow the same naming and namespace patterns

## Import Statements

### For Provider Implementations
```csharp
using Hhs.Shared.Configuration;
using Hhs.Shared.Configuration.Providers.Storage;
using Hhs.Shared.Providers.Storage;
```

### For Consumer Services
```csharp
using Hhs.Shared.Configuration;
using Hhs.Shared.Configuration.Providers.Storage;
using Hhs.Shared.Providers;
using Hhs.Shared.Providers.Storage;
```

## Migration Guide

If you had code using the old structure:

**OLD:**
```csharp
using Hhs.Shared.Configuration;

var factory = new CdnStorageProviderFactory(...);
var provider = factory.CreateProvider("S3");
```

**NEW:**
```csharp
using Hhs.Shared.Providers.Storage;
using Hhs.Shared.Configuration.Providers.Storage;

var factory = new CdnStorageProviderFactory(...);
var provider = factory.CreateProvider("S3");
```

The factory location has moved from `Configuration` to `Providers.Storage`, and settings have moved to `Configuration.Providers.Storage`.

## Benefits of This Structure

✅ **Clear Separation**: Settings are separate from implementations
✅ **Scalability**: Easy to add new provider types (Caching, Logging, etc.)
✅ **Organization**: Grouped by type and purpose
✅ **Discoverability**: Related files are together
✅ **Consistency**: Naming and structure is predictable
✅ **Maintenance**: Changes to one provider don't affect others
✅ **Testing**: Settings can be tested independently from implementations
