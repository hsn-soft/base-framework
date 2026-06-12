namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage.Bunny;

public class BunnyCdnSelfStorageSettings : CdnStorageSettings;

public class BunnyCdnS3StorageSettings : CdnStorageSettings;

public abstract class CdnStorageSettings
{
    public string Provider { get; set; }
    public string DefaultZoneName { get; set; }
    public string PullZoneUrl { get; set; }

    public string StorageProvider { get; set; }
    public string ApiKey { get; set; }
    public string ApiSecret { get; set; }
    public string ApiBaseUrl { get; set; }
    public string Path { get; set; }
    public string FilePrefix { get; set; }
    //public string Bucket { get; set; }
}

//
// var bunnyCdnSettings = new CdnSetting
// {
//     Provider = "Bunny",
//     //ZoneName = clientId.ToString(),
//     ZoneName = "4fe789ab-0652-4e7b-bd35-07019058081d",
//     PullZoneUrl = ".b-cdn.net/"
//     //PullZoneUrl = ".b-cdn.net/videos/"
// };
//
// var bunnyStorageSettings = new CloudStorageSetting
// {
//     Provider = "Bunny",
//     ApiKey = "491456e1-91d6-411f-a00bc5fe3e57-d3c0-4a95",
//     ApiSecret = null,
//     Path = "",
//     FilePrefix = "/files/videos",
//     ApiBaseUrl = "https://storage.bunnycdn.com",
//     Bucket = "demotechsummus"
// };
//
// var backBlazeCdnSettings = new CloudStorageSetting
// {
//     Provider = "BackBlaze",
//     ApiKey = "003185f0bd638690000000001",
//     ApiSecret = "K003DUxnZ70ljrD9ZF4+MPMWeQf0bzM",
//     Path = "/",
//     FilePrefix = "/files/videos",
//     ApiBaseUrl = "https://s3.eu-central-003.backblazeb2.com",
//     Bucket = "testHhsBucket"
// };