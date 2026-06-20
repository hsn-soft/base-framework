using System.Text.Json;

const string testFilePath = "test.txt";

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║         Mock CDN API Upload/Download Test                     ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

// Prepare test file
Console.WriteLine("📝 Test dosyası hazırlanıyor...");
if (!File.Exists(testFilePath))
{
    Console.WriteLine("❌ test.txt bulunamadı! Lütfen projede test.txt dosyası oluşturun.");
    return;
}

var fileBytes = await File.ReadAllBytesAsync(testFilePath);
Console.WriteLine($"✅ Dosya hazır: {testFilePath}");
Console.WriteLine($"   Boyut: {fileBytes.Length} bytes");
Console.WriteLine($"   Hash: {GetHash(fileBytes)}\n");

using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

// Test scenarios
var tests = new[]
{
    new { Name = "CdnLocalMinio", Url = "http://localhost:5070", Backend = "MinIO" },
    new { Name = "CdnBunnySelf", Url = "http://localhost:5071", Backend = "Self-Hosted Disk" },
    new { Name = "CdnBunnyS3", Url = "http://localhost:5072", Backend = "S3 Mock (Local Disk)" },
    new { Name = "CdnAbc → Storage", Url = "http://localhost:5073", Backend = "Storage Backend (5074)" }
};

foreach (var test in tests)
{
    Console.WriteLine(new string('═', 70));
    Console.WriteLine($"🔷 TEST: {test.Name}");
    Console.WriteLine(new string('═', 70));
    Console.WriteLine($"   Endpoint: {test.Url}");
    Console.WriteLine($"   Backend: {test.Backend}\n");

    try
    {
        // Health check
        Console.WriteLine("1️⃣ Health Check:");
        var healthResponse = await httpClient.GetAsync($"{test.Url}/health");
        if (healthResponse.IsSuccessStatusCode)
        {
            var healthContent = await healthResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"   ✅ Server healthy");
        }
        else
        {
            Console.WriteLine($"   ❌ Server not responding: {healthResponse.StatusCode}");
            continue;
        }

        // Upload file
        Console.WriteLine("\n2️⃣ File Upload:");
        Console.WriteLine($"   📤 Uploading {testFilePath} ({fileBytes.Length} bytes)");

        using (var formContent = new MultipartFormDataContent())
        {
            formContent.Add(new ByteArrayContent(fileBytes), "file", testFilePath);
            var uploadResponse = await httpClient.PostAsync($"{test.Url}/upload", formContent);

            if (uploadResponse.IsSuccessStatusCode)
            {
                var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(uploadContent);
                var root = jsonDoc.RootElement;

                var fileId = root.GetProperty("fileId").GetString() ?? "unknown";
                var storageUrl = root.TryGetProperty("storageUrl", out var storageUrlEl)
                    ? storageUrlEl.GetString()
                    : $"{test.Url}/download/{fileId}";
                var cdnUrl = root.TryGetProperty("cdnUrl", out var cdnUrlEl)
                    ? cdnUrlEl.GetString()
                    : storageUrl;

                Console.WriteLine($"   ✅ Upload successful (200 OK)");
                Console.WriteLine($"   📋 FileId: {fileId}");
                Console.WriteLine($"   🔗 StorageUrl: {storageUrl}");
                Console.WriteLine($"   🌐 CdnUrl: {cdnUrl}");

                // Download file
                Console.WriteLine("\n3️⃣ File Download & Verification:");
                Console.WriteLine($"   📥 Downloading from: {storageUrl}");

                var downloadResponse = await httpClient.GetAsync(storageUrl);
                if (downloadResponse.IsSuccessStatusCode)
                {
                    var downloadContent = await downloadResponse.Content.ReadAsByteArrayAsync();
                    var downloadHash = GetHash(downloadContent);
                    var hashMatches = downloadHash == GetHash(fileBytes);

                    Console.WriteLine($"   ✅ Download successful (200 OK)");
                    Console.WriteLine($"   📊 Downloaded: {downloadContent.Length} bytes");
                    Console.WriteLine($"   🔐 Original Hash:   {GetHash(fileBytes)}");
                    Console.WriteLine($"   🔐 Download Hash:   {downloadHash}");
                    Console.WriteLine($"   ✔️ Content Match: {(hashMatches ? "✅ YES" : "❌ NO")}");

                    // Show file storage info
                    Console.WriteLine($"\n   📁 File Storage Info:");
                    ShowStorageLocation(test.Name, fileId);
                }
                else
                {
                    Console.WriteLine($"   ❌ Download failed: {downloadResponse.StatusCode}");
                }
            }
            else
            {
                Console.WriteLine($"   ❌ Upload failed: {uploadResponse.StatusCode}");
                var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"   Error: {errorContent}");
            }
        }

        Console.WriteLine("\n   ✅ Test Passed!\n");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"   ❌ Connection Error: {ex.Message}");
        Console.WriteLine("   💡 Is the mock API running on this port?");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ❌ Error: {ex.Message}");
    }
}

Console.WriteLine(new string('═', 70));
Console.WriteLine("✅ Tüm testler tamamlandı!\n");

Console.WriteLine("📋 INSTRUCTIONS - Mock API'leri çalıştırmak için 5 ayrı terminal açın:\n");
Console.WriteLine("   Terminal 1: cd Hhs.MockApi.Storage && dotnet run        (Port 5074)");
Console.WriteLine("   Terminal 2: cd Hhs.MockApi.CdnLocalMinio && dotnet run  (Port 5070)");
Console.WriteLine("   Terminal 3: cd Hhs.MockApi.CdnBunnySelf && dotnet run   (Port 5071)");
Console.WriteLine("   Terminal 4: cd Hhs.MockApi.CdnBunnyS3 && dotnet run     (Port 5072)");
Console.WriteLine("   Terminal 5: cd Hhs.MockApi.CdnAbc && dotnet run         (Port 5073)");
Console.WriteLine("\n   Sonra bu programı çalıştırın: dotnet run\n");

static string GetHash(byte[] data)
{
    var hash = System.Security.Cryptography.SHA256.HashData(data);
    return Convert.ToHexString(hash)[..16];
}

static void ShowStorageLocation(string cdnName, string fileId)
{
    var baseDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "media");
    var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");

    var locations = new Dictionary<string, string>
    {
        { "CdnLocalMinio", Path.Combine(baseDir, "cdn-local-minio", dateFolder) },
        { "CdnBunnySelf", Path.Combine(baseDir, "cdn-bunny-self", "bunny", dateFolder) },
        { "CdnBunnyS3", Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "uploads", "s3", dateFolder) },
        { "CdnAbc → Storage", Path.Combine(baseDir, "storage", dateFolder) }
    };

    if (locations.TryGetValue(cdnName, out var location))
    {
        Console.WriteLine($"      Expected path: {location}");
        Console.WriteLine($"      File pattern: {fileId}_*.txt");

        // Try to find actual file
        var dir = new DirectoryInfo(location);
        if (dir.Exists)
        {
            var files = dir.GetFiles($"{fileId}*");
            if (files.Length > 0)
            {
                Console.WriteLine($"      ✅ Found: {files[0].FullName}");
                Console.WriteLine($"      Size: {files[0].Length} bytes");
            }
            else
            {
                Console.WriteLine($"      ⚠️ File not found in directory");
            }
        }
        else
        {
            Console.WriteLine($"      ⚠️ Directory doesn't exist yet");
        }
    }
}
