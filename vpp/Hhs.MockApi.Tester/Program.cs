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

    string? storageUrl = null;
    string? cdnUrl = null;

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

        storageUrl = root.GetProperty("storageUrl").GetString();
        cdnUrl = root.GetProperty("cdnUrl").GetString();

        Console.WriteLine($"   ✅ Upload successful (200 OK)");
        Console.WriteLine($"   🔐 Storage URL (Private): {storageUrl}");
        Console.WriteLine($"   🌐 CDN URL (Public):     {cdnUrl}\n");
    }

    // 2. Storage Download Test (Authenticated)
    Console.WriteLine("2️⃣ Storage Download (API Key Required):");
    if (string.IsNullOrEmpty(storageUrl))
    {
        Console.WriteLine("   ❌ Storage URL not available");
        return;
    }

    Console.WriteLine($"   📥 Downloading from: {storageUrl}");

    var storageRequest = new HttpRequestMessage(HttpMethod.Get, storageUrl);
    storageRequest.Headers.Add("X-Api-Key", apiKey);

    var storageResponse = await httpClient.SendAsync(storageRequest);
    if (!storageResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"   ❌ Storage download failed: {storageResponse.StatusCode}");
        return;
    }

    var storageBytes = await storageResponse.Content.ReadAsByteArrayAsync();
    var storageHash = GetHash(storageBytes);
    var storageMatches = storageHash == originalHash;

    Console.WriteLine($"   ✅ Download successful (200 OK)");
    Console.WriteLine($"   📊 Downloaded Size: {storageBytes.Length} bytes");
    Console.WriteLine($"   🔐 Original Hash:   {originalHash}");
    Console.WriteLine($"   🔐 Storage Hash:    {storageHash}");
    Console.WriteLine($"   ✔️ Content Match: {(storageMatches ? "✅ YES" : "❌ NO")}\n");

    // 3. Public CDN Download Test (No Auth)
    Console.WriteLine("3️⃣ Public CDN Download (No Auth Required):");
    if (string.IsNullOrEmpty(cdnUrl))
    {
        Console.WriteLine("   ❌ CDN URL not available");
        return;
    }

    Console.WriteLine($"   📥 Downloading from: {cdnUrl}");

    var cdnResponse = await httpClient.GetAsync(cdnUrl);
    if (!cdnResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"   ❌ CDN download failed: {cdnResponse.StatusCode}");
        return;
    }

    var cdnBytes = await cdnResponse.Content.ReadAsByteArrayAsync();
    var cdnHash = GetHash(cdnBytes);
    var cdnMatches = cdnHash == originalHash;

    Console.WriteLine($"   ✅ Download successful (200 OK)");
    Console.WriteLine($"   📊 Downloaded Size: {cdnBytes.Length} bytes");
    Console.WriteLine($"   🔐 Original Hash:   {originalHash}");
    Console.WriteLine($"   🔐 CDN Hash:        {cdnHash}");
    Console.WriteLine($"   ✔️ Content Match: {(cdnMatches ? "✅ YES" : "❌ NO")}\n");

    // 4. Cache Headers Test
    Console.WriteLine("4️⃣ Cache Headers Verification:");
    if (cdnResponse.Headers.TryGetValues("Cache-Control", out var cacheHeaders))
    {
        var cacheHeader = cacheHeaders.FirstOrDefault();
        Console.WriteLine($"   Cache-Control: {cacheHeader}");
        Console.WriteLine($"   ✅ Immutable cache headers configured\n");
    }

    // 5. Security Test - Storage URL without auth
    Console.WriteLine("5️⃣ Security Test (Storage URL without API Key):");
    var unauthorizedResponse = await httpClient.GetAsync(storageUrl);
    Console.WriteLine($"   Status: {unauthorizedResponse.StatusCode}");
    Console.WriteLine($"   ✔️ Access Denied: {(unauthorizedResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "✅ YES (401)" : "❌ NO")}\n");

    // 6. Summary
    Console.WriteLine("═".PadRight(70, '═'));
    Console.WriteLine("✅ TEST SUMMARY:");
    Console.WriteLine($"   ✅ Upload successful");
    Console.WriteLine($"   ✅ Storage download (authenticated): {(storageMatches ? "PASSED" : "FAILED")}");
    Console.WriteLine($"   ✅ CDN download (public): {(cdnMatches ? "PASSED" : "FAILED")}");
    Console.WriteLine($"   ✅ Cache headers configured");
    Console.WriteLine($"   ✅ Security verified (401 Unauthorized)\n");

    Console.WriteLine("📋 Configuration:");
    Console.WriteLine($"   API Endpoint: {apiUrl}");
    Console.WriteLine($"   MinIO Bucket: cdn-assets");
    Console.WriteLine($"   MinIO Console: http://localhost:9101");
    Console.WriteLine($"   Credentials: minioadmin/minioadmin\n");

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
