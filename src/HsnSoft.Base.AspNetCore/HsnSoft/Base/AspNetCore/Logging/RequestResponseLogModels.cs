using System;
using System.Collections.Generic;
using HsnSoft.Base.Logging.Abstracts;
using JetBrains.Annotations;

namespace HsnSoft.Base.AspNetCore.Logging;

public sealed class RequestResponseLogModel : IRequestResponseLog
{
    [CanBeNull] public string TraceId { get; set; }
    [CanBeNull] public string CorrelationId { get; set; }
    public RequestResponseLogFacility Facility => RequestResponseLogFacility.HTTP_REQUEST_RESPONSE_LOG;

    public ClientInfoLogDetail ClientInfo { get; set; } = new();
    public RequestInfoLogDetail RequestInfo { get; set; } = new();
    public ResponseInfoLogDetail ResponseInfo { get; set; } = new();

    public long RequestResponseWorkingTime { get; set; }
}

public sealed class RequestLogModel : IRequestResponseLog
{
    [CanBeNull] public string TraceId { get; set; }
    [CanBeNull] public string CorrelationId { get; set; }
    public RequestResponseLogFacility Facility => RequestResponseLogFacility.HTTP_REQUEST_LOG;

    public ClientInfoLogDetail ClientInfo { get; set; } = new();
    public RequestInfoLogDetail RequestInfo { get; set; } = new();
}

public sealed class ResponseLogModel : IRequestResponseLog
{
    [CanBeNull] public string TraceId { get; set; }
    [CanBeNull] public string CorrelationId { get; set; }
    public RequestResponseLogFacility Facility => RequestResponseLogFacility.HTTP_RESPONSE_LOG;

    public ResponseInfoLogDetail ResponseInfo { get; set; } = new();

    public long RequestResponseWorkingTime { get; set; }
}

public sealed class ClientInfoLogDetail
{
    [CanBeNull] public string RemoteIp { get; set; }
    [CanBeNull] public string ForwardedFor { get; set; }
    [CanBeNull] public string UserAgent { get; set; }
    [CanBeNull] public string DeviceType { get; set; }
    [CanBeNull] public string AcceptLanguage { get; set; }
    [CanBeNull] public string Origin { get; set; }
    [CanBeNull] public string Referer { get; set; }

    [CanBeNull] public string UserId { get; set; }
    [CanBeNull] public string UserRoles { get; set; }
    [CanBeNull] public string ClientLat { get; set; }
    [CanBeNull] public string ClientLong { get; set; }
    [CanBeNull] public string ClientChannel { get; set; }
    [CanBeNull] public string ClientVersion { get; set; }
}

public sealed class RequestInfoLogDetail
{
    public DateTime RequestDateTimeUtc { get; set; }= DateTime.UtcNow;
    [CanBeNull] public string RequestMethod { get; set; }
    [CanBeNull] public string RequestPath { get; set; }
    [CanBeNull] public string RequestQuery { get; set; }
    [CanBeNull] public string RequestScheme { get; set; }
    [CanBeNull] public string RequestHost { get; set; }
    [CanBeNull] public string RequestContentType { get; set; }
    public long RequestContentLength { get; set; }
    [CanBeNull] public Dictionary<string, string> RequestHeaders { get; set; }
    [CanBeNull] public string RequestBody { get; set; }
}

public sealed class ResponseInfoLogDetail
{
    public DateTime ResponseDateTimeUtc { get; set; }= DateTime.UtcNow;
    public int ResponseStatus { get; set; }
    [CanBeNull] public string ResponseContentType { get; set; }
    public long ResponseContentLength { get; set; }
    [CanBeNull] public Dictionary<string, string> ResponseHeaders { get; set; }
    [CanBeNull] public string ResponseBody { get; set; }
}