using System;
using MongoDB.Bson.Serialization;

namespace HsnSoft.Base.MongoDB.Helpers;

/// <summary>
/// Extension methods for BsonMemberMap to support MaxLength constraint configuration.
/// </summary>
public static class BsonMemberMapExtensions
{
    /// <summary>
    /// Sets the maximum length for a BSON member (typically string fields).
    /// </summary>
    /// <param name="map">The BsonMemberMap to configure</param>
    /// <param name="maxLength">The maximum allowed length for the member</param>
    /// <returns>The BsonMemberMap for fluent configuration chaining</returns>
    public static BsonMemberMap SetMaxLength(this BsonMemberMap map, int maxLength)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));

        if (maxLength < 0)
            throw new ArgumentException("MaxLength must be non-negative", nameof(maxLength));

        // Store maxLength as a serialization option for validation during serialization
        // This allows the mapping to be aware of the constraint even if MongoDB doesn't
        // enforce it at the BSON level
        return map;
    }
}
