namespace Hsx.Shared.Helper;

/// <summary>What a piece of content is: a page of a customer's site, or the daily video made of the most read of them.</summary>
public enum ContentTypes
{
    Unknown = 0,

    /// <summary>One page of a customer's site, made into a video of its own.</summary>
    CustomerContent = 1,

    /// <summary>The video of a day made of the most read pages of a customer.</summary>
    AnalysisContent = 2,
}
