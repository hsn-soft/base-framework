namespace Xyz.ShipmentService.Domain.ShipmentHistoryDomain.Consts;

public static class ShipmentHistoryConsts
{
    public const string TableName = "ShipmentHistory";

    public const int TrackingNumberMaxLength = 32;
    public const int OutcomeMaxLength = 32;
    public const int NoteMaxLength = 500;

    /// <summary>The parcel reached the customer.</summary>
    public const string Delivered = "delivered";

    /// <summary>The parcel did not reach the customer (it was returned).</summary>
    public const string Failed = "failed";
}
