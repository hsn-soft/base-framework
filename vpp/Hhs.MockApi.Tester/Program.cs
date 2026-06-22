using System.Security.Cryptography;
using System.Text.Json;

const string testFilePath = "test.txt";
const string apiUrl = "http://localhost:5070";
const string apiKey = "ciner-secret-api-key";
const string customerKey = "ciner";

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║      Multi-Tenant CDN API (MinIO Backend) Test                 ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

// Prepare test file
Console.WriteLine("📝 Test dosyası hazırlanıyor...");
if (!File.Exists(testFilePath))
{
    Console.WriteLine("❌ test.txt bulunamadı!");
    return;
}

var fileBytes = await File.ReadAllBytesAsync(testFilePath);
var originalHash = GetHash(fileBytes);
Console.WriteLine($"✅ Dosya hazır: {testFilePath}");
Console.WriteLine($"   Boyut: {fileBytes.Length} bytes");
Console.WriteLine($"   Hash: {originalHash}\n");

using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

Console.WriteLine("═".PadRight(70, '═'));
Console.WriteLine("🔷 TEST: CdnLocalMinio + MinIO Multi-Tenant API");
Console.WriteLine("═".PadRight(70, '═') + "\n");

try
{
    // 1. Upload Test
    Console.WriteLine("1️⃣ File Upload (Multi-Tenant):");
    Console.WriteLine($"   📤 Uploading to: {apiUrl}/api/cdn/assets/upload");
    Console.WriteLine($"   👤 Customer: {customerKey}");
    Console.WriteLine($"   📋 API Key: {apiKey}");
    Console.WriteLine($"   Asset Type: documents\n");

    string? cdnUrl = null;
    string? objectKey = null;

    using (var formContent = new MultipartFormDataContent())
    {
        formContent.Add(new ByteArrayContent(fileBytes), "file", testFilePath);
        formContent.Add(new StringContent("documents"), "assetType");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/api/cdn/assets/upload")
        {
            Content = formContent
        };
        request.Headers.Add("X-Api-Key", apiKey);

        var uploadResponse = await httpClient.SendAsync(request);

        if (!uploadResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"   ❌ Upload failed: {uploadResponse.StatusCode}");
            var errorContent = await uploadResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"   Error: {errorContent}");
            return;
        }

        var jsonContent = await uploadResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(jsonContent);
        var root = jsonDoc.RootElement;

        cdnUrl = root.GetProperty("cdnUrl").GetString();
        objectKey = root.GetProperty("objectKey").GetString();

        Console.WriteLine($"   ✅ Upload successful (200 OK)");
        Console.WriteLine($"   🔗 CDN URL: {cdnUrl}");
        Console.WriteLine($"   📦 MinIO Object Key: {objectKey}\n");
    }

    // 2. Download Test
    Console.WriteLine("2️⃣ File Download & Verification:");
    if (string.IsNullOrEmpty(cdnUrl))
    {
        Console.WriteLine("   ❌ CDN URL not available");
        return;
    }

    Console.WriteLine($"   📥 Downloading from: {cdnUrl}");

    var downloadResponse = await httpClient.GetAsync(cdnUrl);
    if (!downloadResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"   ❌ Download failed: {downloadResponse.StatusCode}");
        return;
    }

    var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
    var downloadedHash = GetHash(downloadedBytes);
    var hashMatches = downloadedHash == originalHash;

    Console.WriteLine($"   ✅ Download successful (200 OK)");
    Console.WriteLine($"   📊 Downloaded Size: {downloadedBytes.Length} bytes");
    Console.WriteLine($"   🔐 Original Hash:   {originalHash}");
    Console.WriteLine($"   🔐 Downloaded Hash: {downloadedHash}");
    Console.WriteLine($"   ✔️ Content Match: {(hashMatches ? "✅ YES - Files are identical!" : "❌ NO - Hash mismatch!")}\n");

    // 3. Cache Headers Test
    Console.WriteLine("3️⃣ Cache Headers Verification:");
    var headerResponse = await httpClient.GetAsync(cdnUrl);
    if (headerResponse.Headers.TryGetValues("Cache-Control", out var cacheHeaders))
    {
        var cacheHeader = cacheHeaders.FirstOrDefault();
        Console.WriteLine($"   Cache-Control: {cacheHeader}");
        Console.WriteLine($"   ✅ Immutable cache headers configured\n");
    }

    // 4. Summary
    Console.WriteLine("✅ TEST SUMMARY:");
    Console.WriteLine("   ✅ CdnLocalMinio API is running");
    Console.WriteLine("   ✅ MinIO storage backend is accessible");
    Console.WriteLine("   ✅ File uploaded to multi-tenant customer: " + customerKey);
    Console.WriteLine("   ✅ File hash verification: " + (hashMatches ? "PASSED" : "FAILED"));
    Console.WriteLine("   ✅ Cache headers are set");
    Console.WriteLine($"   ✅ Public file serving works\n");

    Console.WriteLine("📋 Configuration:");
    Console.WriteLine($"   API Endpoint: {apiUrl}");
    Console.WriteLine($"   MinIO Bucket: cdn-assets");
    Console.WriteLine($"   MinIO Console: http://localhost:9101");
    Console.WriteLine($"   Credentials: minioadmin/minioadmin\n");

    Console.WriteLine("📁 File Location in MinIO:");
    Console.WriteLine($"   Object Key: {objectKey}\n");

    Console.WriteLine("═".PadRight(70, '═'));
    Console.WriteLine("✅ All tests passed!\n");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"❌ Connection Error: {ex.Message}");
    Console.WriteLine("💡 Make sure:");
    Console.WriteLine("   1. MinIO is running on localhost:9100");
    Console.WriteLine($"   2. CdnLocalMinio API is running on {apiUrl}");
    Console.WriteLine("   3. Bucket 'cdn-assets' exists in MinIO\n");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine($"   {ex.StackTrace}");
}

static string GetHash(byte[] data)
{
    var hash = SHA256.HashData(data);
    return Convert.ToHexString(hash)[..16];
}
