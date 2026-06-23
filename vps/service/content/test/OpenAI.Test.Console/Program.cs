using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace OpenAI.Test.Console;

internal static class Program
{
    private const string OpenAiApiBaseUrl = "https://api.openai.com";
    private const string OpenAiApiKey = "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";
    private const string OpenAiApiEngine = "gpt-3.5-turbo";

    public static async Task<int> Main(string[] args)
    {
        try
        {
            const string htmlContent = "<div><span>Trabzonspor yattara yı transfer etti</span></div>";

            await ConvertToNormalizedContent(htmlContent);

            return 0;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex.Message);
            return 1;
        }
    }

    private static async Task ConvertToNormalizedContent(string htmlContent)
    {
        var testOpenAIClient = new HttpClient { BaseAddress = new Uri(OpenAiApiBaseUrl) };

        string chatPrompt = "Could you please summarize the html content in Turkish with 5 sentences.";

        testOpenAIClient.DefaultRequestHeaders.Accept.Clear();
        testOpenAIClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        testOpenAIClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenAiApiKey);

        var chatRequest = new OpenAIRequest(OpenAiApiEngine, chatPrompt + " \n" + htmlContent);

        string jsonData = JsonConvert.SerializeObject(chatRequest);
        var result = await testOpenAIClient.PostAsync("/v1/chat/completions", new StringContent(jsonData, Encoding.UTF8, "application/json"));
        string resJson = await result.Content.ReadAsStringAsync();
        if (!result.IsSuccessStatusCode)
        {
            var errorResponse = JsonConvert.DeserializeObject<OpenAIErrorResponse>(resJson);
            throw new Exception(errorResponse.Error.Message);
        }

        var returnObject = JsonConvert.DeserializeObject<OpenAIResponse>(resJson);
        System.Console.WriteLine(returnObject.Choices[0].Message.content);
    }
}