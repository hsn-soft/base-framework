using System.Security.Cryptography;
using System.Text.Json;

const string testFilePath = "test.txt";
const string apiUrl = "http://localhost:5070";
const string apiKey = "ciner-secret-api-key";
const string customerKey = "ciner";

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║      CDN API - Authenticated vs Public Download Test           ║");
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

try
{
    // 1️⃣ UPLOAD
    Console.WriteLine("═".PadRight(70, '═'));
    Console.WriteLine("1️⃣ FILE UPLOAD");
    Console.WriteLine("═".PadRight(70, '═'));
    Console.WriteLine($"📤 Endpoint: POST {apiUrl}/api/cdn/assets/upload");
    Console.WriteLine($"🔐 Auth: X-Api-Key: {apiKey}\n");

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
            Console.WriteLine($"❌ Upload failed: {uploadResponse.StatusCode}");
            return;
        }

        var jsonContent = await uploadResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(jsonContent);
        var root = jsonDoc.RootElement;

        var objectKey = root.GetProperty("objectKey").GetString();
        cdnUrl = root.GetProperty("cdnUrl").GetString();

        // Provider converts CDN response to standardized format
        storageUrl = $"{apiUrl}/api/cdn/assets/download?key={Uri.EscapeDataString(objectKey)}";

        Console.WriteLine($"✅ Upload successful (200 OK)\n");
        Console.WriteLine($"CDN Response:");
        Console.WriteLine($"  ObjectKey: {objectKey}");
        Console.WriteLine($"  CdnUrl:    {cdnUrl}");
        Console.WriteLine($"\nProvider converts to:");
        Console.WriteLine($"  StorageUrl: {storageUrl}");
        Console.WriteLine($"  CdnUrl:     {cdnUrl}\n");
    }

    // 2️⃣ STORAGE DOWNLOAD (WITH API KEY)
    Console.WriteLine("═".PadRight(70, '═'));
    Console.WriteLine("2️⃣ STORAGE DOWNLOAD (With API Key)");
    Console.WriteLine("═".PadRight(70, '═'));
    Console.WriteLine($"📥 Endpoint: GET /api/cdn/assets/download?key=...");
    Console.WriteLine($"🔐 Auth: X-Api-Key: {apiKey}\n");

    var storageRequest = new HttpRequestMessage(HttpMethod.Get, storageUrl);
    storageRequest.Headers.Add("X-Api-Key", apiKey);

    var storageResponse = await httpClient.SendAsync(storageRequest);
    if (!storageResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"❌ Storage download failed: {storageResponse.StatusCode}");
        return;
    }

    var storageBytes = await storageResponse.Content.ReadAsByteArrayAsync();
    var storageHash = GetHash(storageBytes);
    var storageMatch = storageHash == originalHash;

    Console.WriteLine($"✅ Download successful (200 OK)");
    Console.WriteLine($"  Size: {storageBytes.Length} bytes");
    Console.WriteLine($"  Original Hash:   {originalHash}");
    Console.WriteLine($"  Downloaded Hash: {storageHash}");
    Console.WriteLine($"  Match: {(storageMatch ? "✅ YES" : "❌ NO")}\n");

    // 3️⃣ CDN PUBLIC DOWNLOAD (NO API KEY)
    Console.WriteLine("═".PadRight(70, '═'));
    Console.WriteLine("3️⃣ CDN PUBLIC DOWNLOAD (No API Key)");
    Console.WriteLine("═".PadRight(70, '═'));
    Console.WriteLine($"📥 Endpoint: GET {cdnUrl}");
    Console.WriteLine($"🔓 Auth: None\n");

    var cdnResponse = await httpClient.GetAsync(cdnUrl);
    if (!cdnResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"❌ CDN download failed: {cdnResponse.StatusCode}");
        return;
    }

    var cdnBytes = await cdnResponse.Content.ReadAsByteArrayAsync();
    var cdnHash = GetHash(cdnBytes);
    var cdnMatch = cdnHash == originalHash;

    Console.WriteLine($"✅ Download successful (200 OK)");
    Console.WriteLine($"  Size: {cdnBytes.Length} bytes");
    Console.WriteLine($"  Original Hash: {originalHash}");
    Console.WriteLine($"  Downloaded Hash: {cdnHash}");
    Console.WriteLine($"  Match: {(cdnMatch ? "✅ YES" : "❌ NO")}\n");

    // 4️⃣ COMPARISON
    Console.WriteLine("═".PadRight(70, '═'));
    Console.WriteLine("4️⃣ DOWNLOAD METHOD COMPARISON");
    Console.WriteLine("═".PadRight(70, '═'));

    var storageVsCdn = storageHash == cdnHash;
    Console.WriteLine($"Storage URL vs CDN URL files identical: {(storageVsCdn ? "✅ YES" : "❌ NO")}\n");

    // 5️⃣ SECURITY TEST
    Console.WriteLine("═".PadRight(70, '═'));
    Console.WriteLine("5️⃣ SECURITY TEST - Storage URL without API Key");
    Console.WriteLine("═".PadRight(70, '═'));

    var unauthorizedResponse = await httpClient.GetAsync(storageUrl);
    var isUnauthorized = unauthorizedResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized;

    Console.WriteLine($"GET {storageUrl}");
    Console.WriteLine($"Response Status: {unauthorizedResponse.StatusCode}");
    Console.WriteLine($"✅ Access Denied (401): {(isUnauthorized ? "YES ✅" : "NO ❌")}\n");

    // 6️⃣ SUMMARY
    Console.WriteLine("═".PadRight(70, '═'));
    Console.WriteLine("✅ TEST SUMMARY");
    Console.WriteLine("═".PadRight(70, '═'));
    Console.WriteLine($"✅ Upload:                    SUCCESS");
    Console.WriteLine($"✅ Storage Download (Auth):   {(storageMatch ? "PASS ✅" : "FAIL ❌")}");
    Console.WriteLine($"✅ CDN Download (Public):     {(cdnMatch ? "PASS ✅" : "FAIL ❌")}");
    Console.WriteLine($"✅ File Integrity:            {(storageVsCdn ? "MATCH ✅" : "MISMATCH ❌")}");
    Console.WriteLine($"✅ Security (401 check):      {(isUnauthorized ? "PROTECTED ✅" : "EXPOSED ❌")}\n");

    if (storageMatch && cdnMatch && storageVsCdn && isUnauthorized)
    {
        Console.WriteLine("🎉 ALL TESTS PASSED!\n");
    }
    else
    {
        Console.WriteLine("⚠️ SOME TESTS FAILED\n");
    }

    Console.WriteLine("📋 Endpoints:");
    Console.WriteLine($"  Public CDN:  GET  /{customerKey}/documents/...");
    Console.WriteLine($"  Authenticated: GET  /api/cdn/assets/download?key=...  + X-Api-Key header");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"❌ Connection Error: {ex.Message}");
    Console.WriteLine("💡 Make sure CdnLocalMinio API is running on http://localhost:5070\n");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}

static string GetHash(byte[] data)
{
    var hash = SHA256.HashData(data);
    return Convert.ToHexString(hash)[..16];
}
