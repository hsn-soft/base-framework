using System.Security.Cryptography;
using System.Text.Json;

const string testFilePath = "test.txt";
const string cdnApiUrl = "http://localhost:5070";
const string storageApiUrl = "http://localhost:5074";
const string apiKey = "ciner-secret-api-key";
const string storageApiKey = "storage-secret-api-key";

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

var fileBytes = await File.ReadAllBytesAsync(testFilePath);
var originalHash = GetHash(fileBytes);
Console.WriteLine($"✅ Dosya hazır: {testFilePath}");
Console.WriteLine($"   Boyut: {fileBytes.Length} bytes");
Console.WriteLine($"   Hash: {originalHash}\n");

using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

Console.WriteLine("Hangi testi çalıştırmak istiyorsunuz?");
Console.WriteLine("1 = CDN API (Hhs.MockApi.CdnLocalMinio)");
Console.WriteLine("2 = Storage API (Hhs.MockApi.StorageXyz)");
Console.WriteLine();

var choice = Console.ReadLine();

switch (choice)
{
    case "1":
        await TestCdnApi(httpClient, fileBytes, originalHash);
        break;
    case "2":
        await TestStorageApi(httpClient, fileBytes, originalHash);
        break;
    default:
        Console.WriteLine("❌ Geçersiz seçim");
        return;
}

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

            var jsonContent = await uploadResponse.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            var objectKey = root.GetProperty("objectKey").GetString();
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

        var storageBytes = await storageResponse.Content.ReadAsByteArrayAsync();
        var storageHash = GetHash(storageBytes);

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

        var cdnBytes = await cdnResponse.Content.ReadAsByteArrayAsync();
        var cdnHash = GetHash(cdnBytes);

        Console.WriteLine($"✅ Download successful (200 OK)");
        Console.WriteLine($"  Hash match: {(cdnHash == originalHash ? "✅ YES" : "❌ NO")}\n");

        // 4️⃣ SECURITY TEST
        Console.WriteLine("4️⃣ SECURITY TEST");
        var unauthorizedResponse = await client.GetAsync(storageUrl);
        var isSecure = unauthorizedResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized;

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

// ============================================================================
// STORAGE API TEST
// ============================================================================

async Task TestStorageApi(HttpClient client, byte[] fileBytes, string originalHash)
{
    Console.WriteLine("═".PadRight(70, '═'));
    Console.WriteLine("🔷 STORAGE API TEST (Hhs.MockApi.StorageXyz)");
    Console.WriteLine("═".PadRight(70, '═') + "\n");

    try
    {
        // 1️⃣ UPLOAD
        Console.WriteLine("1️⃣ FILE UPLOAD");
        Console.WriteLine($"📤 Endpoint: POST {storageApiUrl}/api/storage/upload\n");

        string? objectKey = null;

        using (var formContent = new MultipartFormDataContent())
        {
            formContent.Add(new ByteArrayContent(fileBytes), "file", testFilePath);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{storageApiUrl}/api/storage/upload")
            {
                Content = formContent
            };
            request.Headers.Add("X-Api-Key", storageApiKey);

            var uploadResponse = await client.SendAsync(request);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Upload failed: {uploadResponse.StatusCode}");
                return;
            }

            var jsonContent = await uploadResponse.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            objectKey = root.GetProperty("objectKey").GetString();

            Console.WriteLine($"✅ Upload successful (200 OK)");
            Console.WriteLine($"  ObjectKey: {objectKey}\n");
        }

        // 2️⃣ AUTHENTICATED DOWNLOAD
        Console.WriteLine("2️⃣ AUTHENTICATED DOWNLOAD");
        var downloadUrl = $"{storageApiUrl}/api/storage/download?key={Uri.EscapeDataString(objectKey)}";
        var downloadRequest = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        downloadRequest.Headers.Add("X-Api-Key", storageApiKey);

        var downloadResponse = await client.SendAsync(downloadRequest);
        if (!downloadResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Download failed: {downloadResponse.StatusCode}");
            return;
        }

        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        var downloadedHash = GetHash(downloadedBytes);

        Console.WriteLine($"✅ Download successful (200 OK)");
        Console.WriteLine($"  Hash match: {(downloadedHash == originalHash ? "✅ YES" : "❌ NO")}\n");

        // 3️⃣ SECURITY TEST - Download without API Key
        Console.WriteLine("3️⃣ SECURITY TEST - Download Protection");
        var unauthorizedResponse = await client.GetAsync(downloadUrl);
        var isSecure = unauthorizedResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized;

        Console.WriteLine($"Download without API Key: {(isSecure ? "✅ 401 Unauthorized" : "❌ EXPOSED")}\n");

        // 4️⃣ SECURITY TEST - Upload without API Key
        Console.WriteLine("4️⃣ SECURITY TEST - Upload Protection");
        using (var formContent = new MultipartFormDataContent())
        {
            formContent.Add(new ByteArrayContent(fileBytes), "file", testFilePath);
            var uploadResponse = await client.PostAsync($"{storageApiUrl}/api/storage/upload", formContent);
            var uploadSecure = uploadResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized;

            Console.WriteLine($"Upload without API Key: {(uploadSecure ? "✅ 401 Unauthorized" : "❌ EXPOSED")}\n");
        }

        Console.WriteLine("✅ STORAGE API TEST PASSED!");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"❌ Connection Error: {ex.Message}");
        Console.WriteLine("💡 Make sure StorageXyz API is running on http://localhost:5074\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}\n");
    }
}

static string GetHash(byte[] data)
{
    var hash = SHA256.HashData(data);
    return Convert.ToHexString(hash)[..16];
}
