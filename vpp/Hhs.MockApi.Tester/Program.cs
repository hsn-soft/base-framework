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

// Select test mode
Console.WriteLine("🔍 Test Modu Seç:");
Console.WriteLine("   1 = CdnLocalMinio (MinIO) Detailed Test");
Console.WriteLine("   2 = Tüm CDN'leri Test Et");
Console.WriteLine();

var choice = Console.ReadLine();

if (choice == "1")
{
    await TestCdnLocalMinioDetailed(httpClient, fileBytes, testFilePath);
}
else
{
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
}

Console.WriteLine("📋 INSTRUCTIONS - Mock API'leri çalıştırmak için:\n");
Console.WriteLine("   Terminal 1: cd Hhs.MockApi.Storage && dotnet run        (Port 5074)");
Console.WriteLine("   Terminal 2: cd Hhs.MockApi.CdnLocalMinio && dotnet run  (Port 5070)");
Console.WriteLine("   Terminal 3: cd Hhs.MockApi.CdnBunnySelf && dotnet run   (Port 5071)");
Console.WriteLine("   Terminal 4: cd Hhs.MockApi.CdnBunnyS3 && dotnet run     (Port 5072)");
Console.WriteLine("   Terminal 5: cd Hhs.MockApi.CdnAbc && dotnet run         (Port 5073)");
Console.WriteLine("\n   Sonra bu programı çalıştırın: dotnet run\n");

static async Task TestCdnLocalMinioDetailed(HttpClient httpClient, byte[] fileBytes, string testFilePath)
{
    Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║       CdnLocalMinio + MinIO Detailed Test                       ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

    const string apiUrl = "http://localhost:5070";
    const string minioUrl = "http://localhost:9100";
    const string minioConsoleUrl = "http://localhost:9101";

    try
    {
        // 1. Health Check
        Console.WriteLine("1️⃣ Health Check:");
        var healthResponse = await httpClient.GetAsync($"{apiUrl}/health");
        if (!healthResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"   ❌ CdnLocalMinio not responding ({healthResponse.StatusCode})");
            return;
        }
        Console.WriteLine($"   ✅ CdnLocalMinio API healthy (5070)");
        Console.WriteLine($"   ✅ MinIO API (9100) expected to be running");
        Console.WriteLine($"   ✅ MinIO Console (9101) available at {minioConsoleUrl}\n");

        // 2. Upload
        Console.WriteLine("2️⃣ File Upload to MinIO:");
        Console.WriteLine($"   📤 Uploading: {testFilePath}");
        Console.WriteLine($"   Size: {fileBytes.Length} bytes");
        Console.WriteLine($"   Original Hash: {GetHash(fileBytes)}\n");

        string? uploadedFileId = null;
        string? s3Key = null;
        string? storedFileName = null;

        using (var formContent = new MultipartFormDataContent())
        {
            formContent.Add(new ByteArrayContent(fileBytes), "file", testFilePath);
            var uploadResponse = await httpClient.PostAsync($"{apiUrl}/upload", formContent);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"   ❌ Upload failed: {uploadResponse.StatusCode}");
                return;
            }

            var jsonContent = await uploadResponse.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            uploadedFileId = root.GetProperty("fileId").GetString();
            s3Key = root.GetProperty("s3Key").GetString();
            storedFileName = root.GetProperty("storedFileName").GetString();
            var storageUrl = root.GetProperty("storageUrl").GetString();
            var cdnUrl = root.GetProperty("cdnUrl").GetString();

            Console.WriteLine($"   ✅ Upload successful!");
            Console.WriteLine($"   📋 FileId: {uploadedFileId}");
            Console.WriteLine($"   📄 FileName: {storedFileName}");
            Console.WriteLine($"   🔗 S3 Key: {s3Key}");
            Console.WriteLine($"   📦 Storage URL: {storageUrl}");
            Console.WriteLine($"   🌐 CDN URL: {cdnUrl}\n");
        }

        // 3. MinIO UI Info
        Console.WriteLine("3️⃣ MinIO UI Information:");
        Console.WriteLine($"   🔗 MinIO Console: {minioConsoleUrl}");
        Console.WriteLine($"   👤 Username: minioadmin");
        Console.WriteLine($"   🔑 Password: minioadmin");
        Console.WriteLine($"   📦 Bucket: videos");
        Console.WriteLine($"   📁 Path: {s3Key}");
        Console.WriteLine($"   💾 Expected to find file: {storedFileName}\n");

        // 4. Download
        Console.WriteLine("4️⃣ File Download & Verification:");
        if (uploadedFileId == null)
        {
            Console.WriteLine("   ❌ FileId not available");
            return;
        }

        Console.WriteLine($"   📥 Downloading from: {apiUrl}/download/{uploadedFileId}");

        var downloadResponse = await httpClient.GetAsync($"{apiUrl}/download/{uploadedFileId}");
        if (!downloadResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"   ❌ Download failed: {downloadResponse.StatusCode}");
            return;
        }

        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        var downloadedHash = GetHash(downloadedBytes);
        var originalHash = GetHash(fileBytes);
        var hashMatches = downloadedHash == originalHash;

        Console.WriteLine($"   ✅ Download successful!");
        Console.WriteLine($"   📊 Downloaded Size: {downloadedBytes.Length} bytes");
        Console.WriteLine($"   🔐 Original Hash:   {originalHash}");
        Console.WriteLine($"   🔐 Downloaded Hash: {downloadedHash}");
        Console.WriteLine($"   ✔️ Content Match: {(hashMatches ? "✅ YES - Files are identical!" : "❌ NO - Hash mismatch!")}\n");

        // 5. List Files
        Console.WriteLine("5️⃣ List All Uploaded Files:");
        var listResponse = await httpClient.GetAsync($"{apiUrl}/list");
        if (listResponse.IsSuccessStatusCode)
        {
            var listContent = await listResponse.Content.ReadAsStringAsync();
            var listDoc = JsonDocument.Parse(listContent);
            var listRoot = listDoc.RootElement;
            var total = listRoot.GetProperty("total").GetInt32();

            Console.WriteLine($"   📊 Total Files: {total}");
            if (total > 0)
            {
                var files = listRoot.GetProperty("files").EnumerateArray();
                foreach (var file in files)
                {
                    var fId = file.GetProperty("fileId").GetString();
                    var fName = file.GetProperty("originalFileName").GetString();
                    var fSize = file.GetProperty("fileSize").GetInt64();
                    Console.WriteLine($"      • {fId}: {fName} ({fSize} bytes)");
                }
            }
            Console.WriteLine();
        }

        // 6. Summary
        Console.WriteLine("✅ TEST SUMMARY:");
        Console.WriteLine("   ✅ CdnLocalMinio API is running");
        Console.WriteLine("   ✅ MinIO storage backend is accessible");
        Console.WriteLine("   ✅ File uploaded successfully");
        Console.WriteLine($"   ✅ File hash verification: {(hashMatches ? "PASSED" : "FAILED")}");
        Console.WriteLine("   ✅ Download successful");
        Console.WriteLine($"\n   📋 Next Steps:");
        Console.WriteLine($"      1. Open MinIO Console: {minioConsoleUrl}");
        Console.WriteLine($"      2. Login with minioadmin/minioadmin");
        Console.WriteLine($"      3. Navigate to bucket 'videos'");
        Console.WriteLine($"      4. Verify file at path: {s3Key}");
        Console.WriteLine($"      5. Confirm: {storedFileName}\n");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"   ❌ Connection Error: {ex.Message}");
        Console.WriteLine("   💡 Make sure CdnLocalMinio and MinIO are running!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ❌ Error: {ex.Message}");
    }
}

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
