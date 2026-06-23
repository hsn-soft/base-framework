using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

const string apiBaseUrl = "https://api.heygen.com";
const string apiKey = "OGZjNzUyYjBiNWMwNGFhNDg5NzllNmE5OGZhNWY5MzgtMTczMzYxNTcyMw==";

Console.WriteLine("Hello, World!");

var result = await GetTemplateFieldsById("a2a2ca3a1d474fbc9489251f0aeb2e7a");
if (result is { Count: > 0 })
{
    foreach (var item in result)
    {
        Console.WriteLine($"{item.Key} , {item.Value}");
    }
}

async Task<List<KeyValuePair<string, string>>> GetTemplateFieldsById(string templateId)
{
    // return new List<KeyValuePair<string, string>>()
    // {
    //     new KeyValuePair<string, string>("publisher_logo", "image"),
    //
    //     new KeyValuePair<string, string>("script_en_0", "text"),
    //     new KeyValuePair<string, string>("script_en_1", "text"),
    //     new KeyValuePair<string, string>("script_en_2", "text"),
    //     new KeyValuePair<string, string>("script_en_3", "text"),
    //     new KeyValuePair<string, string>("script_en_4", "text"),
    //
    //     new KeyValuePair<string, string>("text_0", "text"),
    //     new KeyValuePair<string, string>("text_1", "text"),
    //     new KeyValuePair<string, string>("text_2", "text"),
    //     new KeyValuePair<string, string>("text_3", "text"),
    //     new KeyValuePair<string, string>("text_4", "text"),
    //
    //     new KeyValuePair<string, string>("bg_image_0", "image"),
    //     new KeyValuePair<string, string>("bg_image_1", "image"),
    //     new KeyValuePair<string, string>("bg_image_2", "image"),
    //     new KeyValuePair<string, string>("bg_image_3", "image"),
    //     new KeyValuePair<string, string>("bg_image_4", "image"),
    // };

    var fields = new List<KeyValuePair<string, string>>();
    if (string.IsNullOrWhiteSpace(templateId)) return fields;

    var heyGenClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    string requestUri = "/v2/template/" + templateId;

    heyGenClient.DefaultRequestHeaders.Accept.Clear();
    heyGenClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    heyGenClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

    var result = await heyGenClient.GetAsync(requestUri);
    string resJson = await result.Content.ReadAsStringAsync();
    if (!result.IsSuccessStatusCode)
    {
        dynamic? errorResponse = JsonConvert.DeserializeObject<dynamic>(resJson);
        throw new Exception(errorResponse?.Error.Message);
    }

    var resultJsonObject = JsonConvert.DeserializeObject<JObject>(resJson);
    if (resultJsonObject == null) return fields;

    resultJsonObject.TryGetValue("data", out var dataObject);
    if (dataObject == null) return fields;

    dataObject.As<JObject>().TryGetValue("variables", out var variablesObject);
    if (variablesObject == null) return fields;

    foreach (var property in variablesObject.As<JObject>().Properties())
    {
        property.Value.As<JObject>().TryGetValue("name", out var propertyNameObject);
        string? fieldName = propertyNameObject.As<JValue>().Value?.ToString();

        property.Value.As<JObject>().TryGetValue("type", out var propertyTypeObject);
        string? fieldType = propertyTypeObject.As<JValue>().Value?.ToString();

        fields.Add(new KeyValuePair<string, string>(fieldName ?? string.Empty, fieldType ?? string.Empty));
    }

    return fields;
}

//await UploadStreamToBackBlaze("/Users/serdar.cakir/Downloads/KisaDalgaLogoDark.png");

// async Task<string> UploadStreamToBackBlaze(string inputVideoFileLocation)
// {
//     /*
//     var chain = new CredentialProfileStoreChain();
//     AWSCredentials awsCredentials;
//     AmazonS3Config t = new AmazonS3Config();
//     t.ServiceURL = "https://s3.eu-central-003.backblazeb2.com";
//     //t.EndpointProvider = new AmazonS3EndpointProvider();
//     //AWSConfigs awsConfigs;
//     if (chain.TryGetAWSCredentials("default", out awsCredentials))
//     {
//         Console.WriteLine(AWSConfigs.GetConfig("ServiceURL"));
//         Console.WriteLine(AWSConfigs.GetConfig("AWS_ENDPOINT_URL"));
//         // Use awsCredentials to create an Amazon S3 service client
//         using (var client = new AmazonS3Client(awsCredentials,t))
//         {
//             var response = await client.ListBucketsAsync();
//             Console.WriteLine($"Number of buckets: {response.Buckets.Count}");
//         }
//     }
//     return "";
//     */
// /*
//     AmazonS3Config awsConfig = new AmazonS3Config()
//     {
//         RegionEndpoint = RegionEndpoint.EUCentral1,
//         UseAccelerateEndpoint = false,
//         //ServiceURL = "https://s3.eu-central-003.backblazeb2.com"
//     };
// */
//     /*testHhsBucket
//     var bucket = "testHhsBucket";
//     string AccessKey = "003185f0bd638690000000001";
//     string ApiSecret = "K003DUxnZ70ljrD9ZF4+MPMWeQf0bzM";
//     */
//
//     /*AllBucket*/
//     var bucket = "assets-techsummus"; //demo.techsummus.cpm
//     string AccessKey = "003811b874779640000000001";
//     string ApiSecret = "K003g4G/Wp28X5rJRlXl64DTZxtm81Y";
//
//
// /*
//     var bucket = "4fe789ab-0652-4e7b-bd35-07019058081d"; //demo.techsummus.cpm
//     string AccessKey = "003185f0bd638690000000003";
//     string ApiSecret = "K003kUewtxfnLI2fwdgvOl/iRU9E2Co";
// */
//
//     string endPointUrl = "https://s3.eu-central-003.backblazeb2.com";
//     IAmazonS3 client = CreateS3Client(AccessKey, ApiSecret, endPointUrl, bucket);
//     //Console.WriteLine(client.);
//     // Create a bucket and upload something into it
//     //var bucket = "testHhsBucket"; //cdnSettings.StorageSetting.BunnyCdnSettings.StorageZoneName;
//     try
//     {
//         // The AWS SDK for .NET maps the CreateBucket operation to 'PutBucket'
//         //client.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
//         //var response = await client.ListBucketsAsync();
//         //Console.WriteLine($"Number of buckets: {response.Buckets.First().BucketName}");
//         //Task<PutObjectResponse> response = client.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = Path.GetFileName(inputVideoFileLocation), FilePath = inputVideoFileLocation, ContentType = "image/png"});
//
//         //Task<PutObjectResponse> response = client.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = Path.GetFileName(inputVideoFileLocation), FilePath = inputVideoFileLocation,  ContentType = "video/mp4"});
//         //Console.WriteLine($"Successfully uploaded data to {bucket}{inputVideoFileLocation}:{response.Result}:{response.Status.ToString()}");
//         var request = new GetObjectMetadataRequest
//         {
//             BucketName = bucket,
//             Key = "KisaDalgaLogoDark.png"
//         };
//         var response = await client.GetObjectMetadataAsync(request);
//         Console.WriteLine(response.HttpStatusCode);
//         Console.WriteLine(response.ContentLength);
//
//         return "";
// /*
//         var request = new GetPreSignedUrlRequest
//         {
//             BucketName = "testHhsBucket",
//             Key = Guid.NewGuid().ToString()+".mp4",
//             //Key = "TestVideo.mp4",
//             Verb = HttpVerb.PUT,
//             Expires = DateTime.Now.AddHours(1)
//         };
// */
// /*
//         var getObjectRequest = new GetObjectRequest()
//         {
//             BucketName = bucket,
//             Key = "video.mp4"
//         };
//         var listObjectRequest = new ListObjectsRequest
//         {
//             BucketName = null,
//             Delimiter = null,
//             Encoding = null,
//             ExpectedBucketOwner = null,
//             Marker = null,
//             MaxKeys = 0,
//             Prefix = null,
//             RequestPayer = null,
//             OptionalObjectAttributes = null
//         };
//         var getObjectMetaDataRequest = new GetObjectMetadataRequest
//         {
//             BucketName = bucket,
//             Key = "video.mp4"
//         };
//
//         var getObjectMetadataResponse = await client.GetObjectMetadataAsync(getObjectMetaDataRequest, CancellationToken.None);
//
//
//         var generatePreSignedUrl =  client.GetPreSignedURL(request);
//         Console.WriteLine(generatePreSignedUrl);
//         return generatePreSignedUrl;
//         //return null;
//         */
//     }
//     catch (AmazonS3Exception amazonS3Exception)
//     {
//         if (amazonS3Exception.ErrorCode != null &&
//             (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
//              ||
//              amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
//         {
//             throw new Exception("Check the provided AWS Credentials.");
//         }
//         else
//         {
//             throw new Exception("Error occurred: " + amazonS3Exception.Message);
//         }
//     }
// }

// IAmazonS3 CreateS3Client(string accessKey, string secretKey, string endPointUrl, string credentialProfile)
// {
//     AWSCredentials awsCredentials;
//     var chain = new CredentialProfileStoreChain();
//     if (chain.TryGetAWSCredentials(credentialProfile, out awsCredentials))
//     {
//         Console.WriteLine("Credentials profile found...");
//     }
//     else
//     {
//         Console.WriteLine("Could not find credentials profile. Creating...");
//
//         var options = new CredentialProfileOptions
//         {
//             AccessKey = accessKey,
//             SecretKey = secretKey
//         };
//         var profile = new CredentialProfile(credentialProfile, options);
//         //profile.Region = RegionEndpoint.EUCentral1;
//         var sharedFile = new SharedCredentialsFile();
//         sharedFile.RegisterProfile(profile);
//         if (chain.TryGetAWSCredentials(credentialProfile, out awsCredentials))
//         {
//             return new AmazonS3Client(awsCredentials);
//         }
//     }
//
//     return new AmazonS3Client(awsCredentials);
// }
//curl -v --upload-file  'talkingPreview.mp4' 'https://s3.eu-central-003.backblazeb2.com/testHhsBucket/d55108ea-18af-4992-aa2c-2bed5b2ee594.mp4?X-Amz-Expires=3600&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=003185f0bd638690000000001%2F20241024%2Feu-central-003%2Fs3%2Faws4_request&X-Amz-Date=20241024T150646Z&X-Amz-SignedHeaders=host&X-Amz-Signature=cf4a1406246336bec13948c7f914a57dfe244f9b059aa751987b01e1f635e853'