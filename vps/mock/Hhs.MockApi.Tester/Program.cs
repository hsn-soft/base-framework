using System.Security.Cryptography;
using System.Text.Json;

const string testFilePath = "test.txt";
const string cdnApiUrl = "http://localhost:5070";
const string apiKey = "techsummus-cdn-secret-api-key";

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║         Mock API Test Suite - Choose Your Test                 ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

// Prepare test file
Console.WriteLine("📝 Test dosyası hazırlanıyor...");
if (!File.Exists(testFilePath))
{
    Console.WriteLine("❌ test.txt bulunamadı!");
    return;
}

byte[] fileBytes = await File.ReadAllBytesAsync(testFilePath);
string originalHash = GetHash(fileBytes);
Console.WriteLine($"✅ Dosya hazır: {testFilePath}");
Console.WriteLine($"   Boyut: {fileBytes.Length} bytes");
Console.WriteLine($"   Hash: {originalHash}\n");

using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

Console.WriteLine("CDN API Test çalıştırılıyor...\n");
await TestCdnApi(httpClient, fileBytes, originalHash);

Console.WriteLine("\n✅ Test tamamlandı!\n");

// ============================================================================
// CDN API TEST
// ============================================================================

async Task TestCdnApi(HttpClient client, byte[] fileBytes, string originalHash)
{
    Console.WriteLine("═".PadRight(70, '═'));
    Console.WriteLine("🔷 CDN API TEST (Hhs.MockApi.CdnLocalMinio)");
    Console.WriteLine("═".PadRight(70, '═') + "\n");

    try
    {
        // 1️⃣ UPLOAD
        Console.WriteLine("1️⃣ FILE UPLOAD");
        Console.WriteLine($"📤 Endpoint: POST {cdnApiUrl}/api/cdn/assets/upload\n");

        string? storageUrl = null;
        string? cdnUrl = null;

        using (var formContent = new MultipartFormDataContent())
        {
            formContent.Add(new ByteArrayContent(fileBytes), "file", testFilePath);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{cdnApiUrl}/api/cdn/assets/upload")
            {
                Content = formContent
            };
            request.Headers.Add("X-Api-Key", apiKey);

            var uploadResponse = await client.SendAsync(request);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Upload failed: {uploadResponse.StatusCode}");
                return;
            }

            string jsonContent = await uploadResponse.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            string? objectKey = root.GetProperty("objectKey").GetString();
            cdnUrl = root.GetProperty("cdnUrl").GetString();
            storageUrl = $"{cdnApiUrl}/api/cdn/assets/download?key={Uri.EscapeDataString(objectKey)}";

            Console.WriteLine($"✅ Upload successful (200 OK)");
            Console.WriteLine($"  ObjectKey: {objectKey}");
            Console.WriteLine($"  CdnUrl:    {cdnUrl}");
            Console.WriteLine($"  StorageUrl (for download): {storageUrl}\n");
        }

        // 2️⃣ STORAGE DOWNLOAD (WITH API KEY)
        Console.WriteLine("2️⃣ STORAGE DOWNLOAD (Authenticated)");
        var storageRequest = new HttpRequestMessage(HttpMethod.Get, storageUrl);
        storageRequest.Headers.Add("X-Api-Key", apiKey);

        var storageResponse = await client.SendAsync(storageRequest);
        if (!storageResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Storage download failed: {storageResponse.StatusCode}");
            return;
        }

        byte[] storageBytes = await storageResponse.Content.ReadAsByteArrayAsync();
        string storageHash = GetHash(storageBytes);

        Console.WriteLine($"✅ Download successful (200 OK)");
        Console.WriteLine($"  Hash match: {(storageHash == originalHash ? "✅ YES" : "❌ NO")}\n");

        // 3️⃣ PUBLIC DOWNLOAD
        Console.WriteLine("3️⃣ PUBLIC CDN DOWNLOAD");
        var cdnResponse = await client.GetAsync(cdnUrl);
        if (!cdnResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ CDN download failed: {cdnResponse.StatusCode}");
            return;
        }

        byte[] cdnBytes = await cdnResponse.Content.ReadAsByteArrayAsync();
        string cdnHash = GetHash(cdnBytes);

        Console.WriteLine($"✅ Download successful (200 OK)");
        Console.WriteLine($"  Hash match: {(cdnHash == originalHash ? "✅ YES" : "❌ NO")}\n");

        // 4️⃣ SECURITY TEST
        Console.WriteLine("4️⃣ SECURITY TEST");
        var unauthorizedResponse = await client.GetAsync(storageUrl);
        bool isSecure = unauthorizedResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized;

        Console.WriteLine($"StorageUrl without API Key: {(isSecure ? "✅ 401 Unauthorized" : "❌ EXPOSED")}\n");

        Console.WriteLine("✅ CDN API TEST PASSED!");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"❌ Connection Error: {ex.Message}");
        Console.WriteLine("💡 Make sure CdnLocalMinio API is running on http://localhost:5070\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}\n");
    }
}


static string GetHash(byte[] data)
{
    byte[] hash = SHA256.HashData(data);
    return Convert.ToHexString(hash)[..16];
}
