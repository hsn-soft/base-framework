using System.ComponentModel.DataAnnotations;

namespace Hhs.VideoGeneratorService.Domain.Enums;

public enum VideoRequestStates
{
    [Display(Name = "CreatedWaitForAudioSend")]
    CreatedWaitForAudioSend = 12,

    [Display(Name = "CreatedWaitForVideoSent")]
    CreatedWaitForVideoSent = 13,

    [Display(Name = "AudioSentWaitForAudioGeneration")]
    AudioSentWaitForAudioGeneration = 21,

    [Display(Name = "AudioGeneratedWaitForAudioDownload")]
    AudioGeneratedWaitForAudioDownload = 22,

    [Display(Name = "AudioDownloadedWaitForAudioStorageUpload")]
    AudioDownloadedWaitForAudioStorageUpload = 23,

    [Display(Name = "VideoSentWaitForVideoGeneration")]
    VideoSentWaitForVideoGeneration = 31,

    [Display(Name = "VideoGeneratedWaitForVideoDownload")]
    VideoGeneratedWaitForVideoDownload = 32,

    [Display(Name = "VideoDownloadedWaitForVideoStorageUpload")]
    VideoDownloadedWaitForVideoStorageUpload = 33,

    [Display(Name = "Fail")]
    OperationFail = 51,

    [Display(Name = "SendFail")]
    SendFail = 52,

    [Display(Name = "QueryFail")]
    QueryFail = 53,

    [Display(Name = "Success")]
    OperationSuccess = 99
}