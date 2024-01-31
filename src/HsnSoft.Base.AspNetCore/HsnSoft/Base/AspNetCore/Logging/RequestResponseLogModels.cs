using System;
using JetBrains.Annotations;

namespace HsnSoft.Base.AspNetCore.Logging;

public sealed record RequestLogModel(
    string LogId,
    [CanBeNull] string TraceId,
    [CanBeNull] string CorrelationId,
    string Facility,
    DateTime RequestDateTimeUtc,
    RequestLogDetail Request
) : IRequestResponseLog
{
    public string LogId { get; } = LogId;

    [CanBeNull]
    public string TraceId { get; } = TraceId;

    [CanBeNull]
    public string CorrelationId { get; } = CorrelationId;

    public string Facility { get; } = Facility;

    public DateTime RequestDateTimeUtc { get; } = RequestDateTimeUtc;

    public RequestLogDetail Request { get; } = Request;
}

public sealed record ResponseLogModel(
    string LogId,
    [CanBeNull] string TraceId,
    [CanBeNull] string CorrelationId,
    string Facility,
    DateTime ResponseDateTimeUtc,
    [CanBeNull] ResponseLogDetail Response,
    [CanBeNull] string RequestResponseWorkingTime
) : IRequestResponseLog
{
    public string LogId { get; } = LogId;

    [CanBeNull]
    public string TraceId { get; } = TraceId;

    [CanBeNull]
    public string CorrelationId { get; } = CorrelationId;

    public string Facility { get; } = Facility;
    public DateTime ResponseDateTimeUtc { get; } = ResponseDateTimeUtc;

    [CanBeNull]
    public ResponseLogDetail Response { get; } = Response;

    [CanBeNull]
    public string RequestResponseWorkingTime { get; } = RequestResponseWorkingTime;
}

public sealed record RequestLogDetail(
    [CanBeNull] string ClientIp,
    [CanBeNull] string RequestHost,
    [CanBeNull] string RequestLat,
    [CanBeNull] string RequestLong,
    [CanBeNull] string ClientVersion,
    [CanBeNull] RequestUserDetail UserInfo,
    string RequestPath,
    [CanBeNull] string RequestBody)
{
    [CanBeNull]
    public string ClientIp { get; } = ClientIp;

    [CanBeNull]
    public string RequestHost { get; } = RequestHost;

    [CanBeNull]
    public string RequestLat { get; } = RequestLat;

    [CanBeNull]
    public string RequestLong { get; } = RequestLong;

    [CanBeNull]
    public string ClientVersion { get; } = ClientVersion;

    [CanBeNull]
    public RequestUserDetail UserInfo { get; } = UserInfo;

    public string RequestPath { get; } = RequestPath;

    [CanBeNull]
    public string RequestBody { get; } = RequestBody;
}

public sealed record ResponseLogDetail(
    int ResponseStatus,
    [CanBeNull] string ResponseBody)
{
    public int ResponseStatus { get; } = ResponseStatus;

    [CanBeNull]
    public string ResponseBody { get; } = ResponseBody;
}

public sealed record RequestUserDetail(
    [CanBeNull] string UserId,
    [CanBeNull] string Role)
{
    [CanBeNull]
    public string UserId { get; } = UserId;

    [CanBeNull]
    public string Role { get; } = Role;
}

// Marker
public interface IRequestResponseLog
{
}