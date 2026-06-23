using System.Net.Http.Headers;
using System.Text.Json;

const string RabbitMqHost = "localhost";
const int RabbitMqManagementPort = 15672;
const string RabbitMqUsername = "guest";
const string RabbitMqPassword = "guest";

var handler = new HttpClientHandler();
handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true;

using var httpClient = new HttpClient(handler);
var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{RabbitMqUsername}:{RabbitMqPassword}"));
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

var baseUrl = $"http://{RabbitMqHost}:{RabbitMqManagementPort}/api";

Console.WriteLine("🔄 Starting RabbitMQ reset...\n");

try
{
    // Get all queues
    var queueResponse = await httpClient.GetAsync($"{baseUrl}/queues");
    if (!queueResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"❌ Failed to get queues: {queueResponse.StatusCode}");
        return;
    }

    var queueContent = await queueResponse.Content.ReadAsStringAsync();
    using var jsonDoc = JsonDocument.Parse(queueContent);
    var queueList = jsonDoc.RootElement.EnumerateArray().ToList();

    Console.WriteLine($"📊 Found {queueList.Count} queues\n");

    var deletedCount = 0;
    foreach (var queue in queueList)
    {
        var name = queue.GetProperty("name").GetString() ?? "";
        var vhost = queue.GetProperty("vhost").GetString() ?? "/";

        var deleteResponse = await httpClient.DeleteAsync($"{baseUrl}/queues/{Uri.EscapeDataString(vhost)}/{Uri.EscapeDataString(name)}");

        if (deleteResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"✓ Deleted queue: {name}");
            deletedCount++;
        }
        else
        {
            Console.WriteLine($"✗ Failed to delete queue {name}: {deleteResponse.StatusCode}");
        }
    }

    Console.WriteLine($"\n🎉 Deleted {deletedCount}/{queueList.Count} queues");

    // Get all user-defined exchanges
    var exchangeResponse = await httpClient.GetAsync($"{baseUrl}/exchanges");
    if (!exchangeResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"❌ Failed to get exchanges: {exchangeResponse.StatusCode}");
        return;
    }

    var exchangeContent = await exchangeResponse.Content.ReadAsStringAsync();
    using var exchangeJsonDoc = JsonDocument.Parse(exchangeContent);
    var exchangeList = exchangeJsonDoc.RootElement.EnumerateArray()
        .Where(e => e.GetProperty("name").GetString() != "" && !e.GetProperty("name").GetString()!.StartsWith("amq."))
        .ToList();

    Console.WriteLine($"\n📊 Found {exchangeList.Count} user-defined exchanges\n");

    var exchangeDeletedCount = 0;
    foreach (var exchange in exchangeList)
    {
        var name = exchange.GetProperty("name").GetString() ?? "";
        var vhost = exchange.GetProperty("vhost").GetString() ?? "/";

        var deleteResponse = await httpClient.DeleteAsync($"{baseUrl}/exchanges/{Uri.EscapeDataString(vhost)}/{Uri.EscapeDataString(name)}");

        if (deleteResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"✓ Deleted exchange: {name}");
            exchangeDeletedCount++;
        }
        else
        {
            Console.WriteLine($"✗ Failed to delete exchange {name}: {deleteResponse.StatusCode}");
        }
    }

    Console.WriteLine($"\n🎉 Deleted {exchangeDeletedCount}/{exchangeList.Count} exchanges");
    Console.WriteLine("\n✅ RabbitMQ reset completed!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}
