using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Client.Test.Console;

internal static class Program
{
    private const string ContentApiBaseUrl = "http://localhost:7450";
    private static Guid TenantId => Guid.Parse("97422b81-74da-4532-a230-9e4fb0c0dede");
    private static Guid CustomerId => Guid.Parse("0b16bed1-ffdb-4066-a33d-c6ee074b72e7");

    public static async Task<int> Main(string[] args)
    {
        try
        {
            for (int i = 1; i <= 20; i++)
            {
                await SampleRequest("deneme", i);
            }

            return 0;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex.Message);
            return 1;
        }
    }

    private static async Task SampleRequest(string contentKey, int contentNumber)
    {
        var contentClient = new HttpClient { BaseAddress = new Uri(ContentApiBaseUrl) };

        contentClient.DefaultRequestHeaders.Accept.Clear();
        contentClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var reqBody = new GetOrCreateContentRequestDto
        {
            TenantId = TenantId,
            CustomerId = CustomerId,
            DomainName = "localhost",
            ContentKey = "https://localhost:6161/Test/" + contentKey + "_" + contentNumber
        };

        string jsonData = JsonConvert.SerializeObject(reqBody);

        var result = await contentClient.PostAsync("/api/content-service/v1/commercial/contents/get-or-create-test",
            new StringContent(jsonData, Encoding.UTF8, "application/json"));
        // var resJson = await result.Content.ReadAsStringAsync();
        // System.Console.WriteLine(resJson);

        System.Console.WriteLine(!result.IsSuccessStatusCode ? "Failure: {0}" : "Success: {0}", contentKey + "_" + contentNumber);
    }
}