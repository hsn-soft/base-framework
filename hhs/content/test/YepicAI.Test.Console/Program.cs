using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace YepicAI.Test.Console;

internal static class Program
{
    private const string YepicAiApiBaseUrl = "https://api.yepic.ai";
    private const string YepicAiApiKey = "213957bc-41c8-4f5f-865c-2c341e37166a";

    public static async Task<int> Main(string[] args)
    {
        try
        {
            // var normalizedContent = "Almanya’nın köklü kulübü Borussia Dortmund ile yollarını ayırdıktan sonra Karadeniz ekibine gelen futbolcu, yeni takımındaki serüvenine yaptığı asistle iyi başlarken, hakkında Belçika basınında çıkan haberlerde oyuncunun bu tercihinin milli takım açısından irdelendiği görüldü. Thomas Meunier şaşırtıcı bir şekilde Borussia Dortmund’dan Türkiye’ye transfer oldu. Almanya’da yaklaşan Avrupa Şampiyonası var. Alman kulübü Borussia Dortmund’da ilk 11 pozisyonunda yer alan Thomas Meunier, bu kış Türk Trabzonspor’a transfer olmayı tercih etti. Avrupa Şampiyonası için şansı çok yüksek değildi ama şimdi artık daha da azaldı. 32 yaşındaki sağ bek Gençlerbirliği ile yapılan kupa maçını uzatmaya götüren golün asistini yapmıştı.";
            string normalizedContent = "Bordo-Mavililer, dün sabah yaptığı antrenmanla Hatayspor maçının hazırlıklarını sürdürdü. Mehmet Ali Yılmaz Tesisleri’nde idman, pas-koordinasyon çalışmasıyla başlarken; çift kale maçın ardından sonuçlandırma antrenmanı yapıldı. Teknik direktör Abdullah Avcı yönetimindeki idman yoğun tempoda geçerken, bugün son taktik prova gerçekleşecek ve ardından Hatayspor maçı için kampa girilecek. Borussia Dortmund’tan transfer edildikten sonra ayağının tozuyla Gençlerbirliği maçına çıkan ve kupadaki mücadelenin ikinci yarısında oyuna dahil olup Eren Elmalı’nın golünde asisti yapan Thomas Menuier, yarınki Hatayspor karşılaşmasına ilk 11’de başlayacak. Teknik direktör Abdullah Avcı, tecrübeli sağ bekin uyum sürecinden memnun. Fırtına’nın hocası, Belçikalı futbolcunun savunma hattına liderlik yapmasını istiyor. Afrika Kupası’na gidenler, takımdan ayrılanlar derken Trabzonspor eksiklerin çokluğunda çıktığı son 4 lig maçını da kaybetmişti. Bu süreçte Galatasaray, Rizespor, Kasımpaşa ve son olarak Beşiktaş’a mağlup olan Bordo-Mavililer, artık kazanıp beyaz bir sayfa açmak istiyor. Hafta içinde Türkiye Kupası’nda Gençlerbirliği’ni zor da olsa eleyen ve uzatmalarda rakibini deviren Fırtına, tamamen Hatayspor karşılaşmasına kilitlendi.";

            // var videoJobId = await CreateVideo(normalizedContent);
            string videoJobId = "7b472f4d-b8ca-4cf6-8c82-5d7d9e30f84e";
            await QueryVideo(videoJobId);

            return 0;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex.Message);
            return 1;
        }
    }

    private static async Task<string> CreateVideo(string normalizedContent)
    {
        var testYepicAIClient = new HttpClient { BaseAddress = new Uri(YepicAiApiBaseUrl) };

        testYepicAIClient.DefaultRequestHeaders.Accept.Clear();
        testYepicAIClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        testYepicAIClient.DefaultRequestHeaders.Add("X-Api-Key", YepicAiApiKey);

        var talkingPhoto = new TalkingPhotoRequest
        {
            AvatarId = "ea03926b-a6fb-4bce-88d4-8c6bd35aaf05",
            VoiceId = "tr-TR-AhmetNeural",
            Script = normalizedContent,
            VideoFormat = "mp4",
            VideoWidth = 1080,
            VideoHeight = 1920,
            VideoTitle = "Generic Talking Photo",
            Visibility = "Public"
        };

        string jsonData = JsonConvert.SerializeObject(talkingPhoto);
        // Studio express -> Add a Voice
        var result = await testYepicAIClient.PostAsync("/v1/talkingphotos", new StringContent(jsonData, Encoding.UTF8, "application/json"));
        string resJson = await result.Content.ReadAsStringAsync();
        System.Console.WriteLine(resJson);
        if (!result.IsSuccessStatusCode)
        {
            System.Console.WriteLine($"API Error: {resJson}");
        }

        var returnObject = JsonConvert.DeserializeObject<TalkingPhotoResponse>(resJson);

        string videoJobId = returnObject.Id;

        return videoJobId;
    }

    private static async Task QueryVideo(string videoJobId)
    {
        var testYepicAIClient = new HttpClient { BaseAddress = new Uri(YepicAiApiBaseUrl) };

        testYepicAIClient.DefaultRequestHeaders.Accept.Clear();
        testYepicAIClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        testYepicAIClient.DefaultRequestHeaders.Add("X-Api-Key", YepicAiApiKey);

        // Studio express -> Query a Voice
        for (int i = 1; i <= 20; i++)
        {
            var result = await testYepicAIClient.GetAsync($"/v1/talkingphotos/{videoJobId}");
            string resJson = await result.Content.ReadAsStringAsync();
            if (!result.IsSuccessStatusCode)
            {
                System.Console.WriteLine($"API Error: {resJson}");
            }
            else
            {
                var returnObject = JsonConvert.DeserializeObject<TalkingPhotoResponse>(resJson);
                if (returnObject.DateCreated.HasValue && returnObject.RenderDuration.HasValue)
                {
                    // video is ready
                    System.Console.WriteLine("Video is ready");
                    System.Console.WriteLine($"Video Url: {returnObject.VideoUrl}");
                    System.Console.WriteLine($"Video completed: {returnObject.DateRenderCompleted.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffffzzz")}");
                    break;
                }
                else
                {
                    // video is processing
                    System.Console.WriteLine($"Query Result [{i.ToString()}]: Video is processing");
                }
            }

            await Task.Delay(5000);
        }
    }
}