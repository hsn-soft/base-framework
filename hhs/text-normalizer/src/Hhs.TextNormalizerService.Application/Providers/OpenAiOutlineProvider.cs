using System.ClientModel;
using System.Net.Http.Headers;
using System.Text;
using Hhs.TextNormalizerService.Application.Contracts.Providers;
using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Outline;
using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Outline.OpenAI;
using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Outline.OpenAIResponses;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;
using OpenAI.Chat;

namespace Hhs.TextNormalizerService.Application.Providers;

public class OpenAiOutlineProvider : IOutlineProvider
{
    private readonly IAppConsoleLogger _logger;
    private readonly OpenAiSettings _openAiSettings;
    private const string ApiBaseUrl = "https://api.openai.com";
    private const string apiEngine = "gpt-3.5-turbo";

    public OpenAiOutlineProvider(IAppConsoleLogger logger, IOptions<OpenAiSettings> openAiSettings)
    {
        _logger = logger;
        _openAiSettings = openAiSettings?.Value ?? throw new ArgumentNullException(nameof(openAiSettings));
    }

    public async Task<OutlineResponseDto> OutlineWithStructuredOutputAsync(OutlineRequestDto input, string model = null)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.OutlineInput)) return null;
        try
        {
            var generator = new JSchemaGenerator();
            generator.DefaultRequired = Required.Always; // required by OpenAI
            generator.GenerationProviders.Add(new StringEnumGenerationProvider());
            var schema = generator.Generate(typeof(StructuredOutput));
            schema.AllowAdditionalProperties = false; // required by OpenAI
            var schemaAsString = schema.ToString();
            var options = new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat("responses_schema", new BinaryData(schemaAsString), null, true) };
            /*
            var systemPrompt = """
                               Sen deneyimli bir haber editörüsün. Sana bir haber metni verilecek.
                               Aşağıdaki alanları oluştur ve sadece geçerli JSON formatında döndür:

                               - Category: En fazla 3
                               - Tags: En fazla 10
                               - Description: En fazla 250 karakter
                               - Title: Haber başlığı
                               - Summary: Haber Özeti, ortalama 50-60 kelime
                               """;*/
            List<ChatMessage> messages =
            [
                new SystemChatMessage(input.OutlinePrompt),
                new UserChatMessage(input.OutlineInput)
            ];

            // Send query to OpenAI
            ChatClient client = new("gpt-4o-mini", new ApiKeyCredential(_openAiSettings.ApiKey));
            ChatCompletion completion = await client.CompleteChatAsync(messages, options);
            var responseToPrompt = completion.Content[0].Text;
            return new OutlineResponseDto { RefContentId = input.RefContentId, OutlinedData = responseToPrompt };
        }
        catch (Exception ex)
        {
            return new OutlineResponseDto { RefContentId = input.RefContentId, HasError = true, ErrorDetails = ex.Message };
        }
    }

    public async Task<OutlineResponseDto> OutlineAsync(OutlineRequestDto input, string model = null, bool useStructuredOutput = false)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.OutlinePrompt) || string.IsNullOrWhiteSpace(input.OutlineInput)) return null;
        if (useStructuredOutput)
            return await OutlineWithStructuredOutputAsync(input, model);

        var testOpenAiClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
        testOpenAiClient.DefaultRequestHeaders.Accept.Clear();
        testOpenAiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        testOpenAiClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _openAiSettings.ApiKey);

        try
        {
            var chatRequest = new OpenAIRequest(model ?? apiEngine, input.OutlinePrompt + " \n" + input.OutlineInput);
            string jsonData = JsonConvert.SerializeObject(chatRequest);
            var result = await testOpenAiClient.PostAsync("/v1/chat/completions",
                new StringContent(jsonData, Encoding.UTF8, "application/json"));
            string resJson = await result.Content.ReadAsStringAsync();
            if (!result.IsSuccessStatusCode)
            {
                var errorResponse = JsonConvert.DeserializeObject<OpenAIErrorResponse>(resJson);
                throw new Exception(errorResponse.Error.Message);
            }

            var returnObject = JsonConvert.DeserializeObject<OpenAIResponse>(resJson);
            string checkedContent = returnObject.Choices[0].Message.content
                .Replace("-", " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("\t", " ");

            return new OutlineResponseDto { RefContentId = input.RefContentId, OutlinedData = checkedContent };
        }
        catch (Exception ex)
        {
            return new OutlineResponseDto { RefContentId = input.RefContentId, HasError = true, ErrorDetails = ex.Message };
        }
    }

    public async Task<List<OutlineResponseDto>> OutlineAsync(List<OutlineRequestDto> input)
    {
        var results = new List<OutlineResponseDto>();
        foreach (var outlineRequestDto in input)
        {
            var outlineResult = await OutlineAsync(outlineRequestDto);
            if (outlineResult != null)
            {
                results.Add(outlineResult);
            }
            else
            {
                // outline error
                break;
            }

            // rate limiter wait
            Thread.Sleep(200);
        }

        return results;
    }
}