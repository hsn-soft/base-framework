using System.Net.Http.Headers;
using System.Reflection;
using Hhs.VideoGeneratorService.Application.Contracts.Providers;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Audio.ElevenLabs;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Providers;

public class ElevenLabsAudioProvider : IElevenLabsAudioProvider
{
    private readonly IAppConsoleLogger _logger;
    private readonly IHostEnvironment _env;

    public ElevenLabsAudioProvider(IAppConsoleLogger logger,IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async Task<AudioGenerationSendResponseDto> SendAudioAsync(AudioGenerationSendRequestDto input, ElevenLabsSettings audioProviderSettings)
    {
        if (string.IsNullOrWhiteSpace(input.AudioContent)) return null;
        var elevenLabsClient = new HttpClient { BaseAddress = new Uri(audioProviderSettings.ApiBaseUrl) };

        elevenLabsClient.DefaultRequestHeaders.Accept.Clear();
        elevenLabsClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("audio/mpeg"));
        elevenLabsClient.DefaultRequestHeaders.Add("xi-api-key", audioProviderSettings.ApiKey);

        var data = new
        {
            text = input.AudioContent,
            model_id = audioProviderSettings.Model,
            language_code = audioProviderSettings.LanguageCode,
            voice_settings = new
            {
                stability = audioProviderSettings.Stability,
                similarity_boost = audioProviderSettings.SimilarityBoost,
                style=0.0,
                use_speaker_boost = false,
                speed = audioProviderSettings.Speed
            },
            apply_text_normalization = "on"
        }; // Set-up Data

        // Convert Data to JSON
        string jsonData = JsonConvert.SerializeObject(data, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        var httpContent = new StringContent(jsonData, System.Text.Encoding.Default, "application/json");

        try
        {
            // Request MPEG
            var response = await elevenLabsClient.PostAsync(audioProviderSettings.VoiceId, httpContent);
            // Output Response to local MPEG file in the respective directory

            if (!response.IsSuccessStatusCode) return null;

            int fileNameExtension = 0;
            int retries = 0;
            bool fileNameValid = false;

            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string workingDirectory = Path.GetDirectoryName(path);

            string baseLocation = _env.EnvironmentName is "production" or "stage" ? "/tmp/audios" : workingDirectory + "/files/audios";
            if (!Directory.Exists(baseLocation))
            {
                Directory.CreateDirectory(baseLocation);
            }
            string fileName = $"{baseLocation}/{input.VideoRequestReferenceId}.mp3";

            _logger.LogDebug($"Starting file download to {baseLocation}");

            while (!fileNameValid)
            {
                try
                {
                    // Stream response as binary data into a file
                    await using (var stream = await response.Content.ReadAsStreamAsync())
                    await using (var fileStream = File.Create($"{baseLocation}/{input.VideoRequestReferenceId}-{fileNameExtension}.mp3"))
                    {
                        await stream.CopyToAsync(fileStream);
                    }

                    fileNameValid = true;
                }
                catch (Exception ex)
                {
                    retries++;
                    if (retries >= 5)
                        break;
                    Thread.Sleep(200);
                    fileNameExtension++;
                }
            }

            if (fileNameValid)
            {
                File.Move($"{baseLocation}/{input.VideoRequestReferenceId}-{fileNameExtension}.mp3", fileName);
                return new AudioGenerationSendResponseDto
                {
                    AudioFileName = fileName,
                    HasError = false,
                    ErrorMessage = null
                };
            }

            return new AudioGenerationSendResponseDto
            {
                AudioFileName = null,
                HasError = true,
                ErrorMessage = null
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}