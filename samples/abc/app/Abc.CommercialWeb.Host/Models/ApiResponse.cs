namespace Abc.CommercialWeb.Host.Models;

/// <summary>The answer of every Abc service, as the gateway forwards it (<c>BaseResponse</c>): the data is in <see cref="Payload"/>.</summary>
public sealed class ApiResponse<T>
{
    public int StatusCode { get; init; }

    public List<string> StatusMessages { get; init; } = [];

    public T? Payload { get; init; }
}
