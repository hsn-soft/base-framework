namespace HsnSoft.Base.EventBus.Inbox;

/// <summary>Where a message is in the inbox of a service.</summary>
public static class InboxStatuses
{
    /// <summary>A handling has taken the message and has not ended (yet): it runs, or it died.</summary>
    public const string Started = "Started";

    /// <summary>The message was handled (or the application said no to it): it is never handled again.</summary>
    public const string Completed = "Completed";

    /// <summary>The handling failed: the message is handled again when it comes back, up to <see cref="EventInboxOptions.MaxRetryCount"/> times.</summary>
    public const string Failed = "Failed";
}
