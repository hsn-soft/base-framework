using System.ComponentModel.DataAnnotations;

namespace Hhs.IdentityService.Domain.Enums;

public enum ProductTypes
{
    [Display(Name = "Unknown", Prompt = "unknown")]
    Unknown = 0,

    [Display(Name = "Video Platform", Prompt = "vp")]
    VideoPlatform = 11,

    [Display(Name = "Vertical Video", Prompt = "cc")]
    VerticalVideo = 12,

    [Display(Name = "Podcast", Prompt = "pc")]
    Podcast = 13,

    [Display(Name = "Ad-Wall", Prompt = "aw")]
    AdWall = 14,

    [Display(Name = "Bidding Tech", Prompt = "bt")]
    BiddingTech= 12,

    [Display(Name = "E-Invoice", Prompt = "ei")]
    EInvoice = 12,
}