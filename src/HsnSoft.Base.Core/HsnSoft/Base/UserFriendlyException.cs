using System;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base;

/// <summary>
/// This exception type is directly shown to the user.
/// </summary>
[Serializable]
public class UserFriendlyException : BusinessException, IUserFriendlyException
{
    public UserFriendlyException(
        string message,
        string code = null,
        string details = null,
        Exception innerException = null,
        LogLevel logLevel = LogLevel.Warning)
        : base(
            code,
            message,
            details,
            innerException,
            logLevel)
    {
        ErrorDetails = details;
    }

    /// <summary>
    /// Constructor for serializing.
    /// </summary>
    public UserFriendlyException(SerializationInfo serializationInfo, StreamingContext context)
        : base(serializationInfo, context)
    {
    }
}