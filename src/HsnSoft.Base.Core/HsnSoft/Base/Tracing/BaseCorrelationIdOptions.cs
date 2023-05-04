namespace HsnSoft.Base.Tracing;

public class BaseCorrelationIdOptions
{
    public string HttpHeaderName { get; set; } = "X-Correlation-Id";

    public bool SetResponseHeader { get; set; } = true;
}