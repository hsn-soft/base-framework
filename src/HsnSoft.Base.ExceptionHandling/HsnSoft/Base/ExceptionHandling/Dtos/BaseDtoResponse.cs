namespace HsnSoft.Base.ExceptionHandling.Dtos;

public sealed class BaseDtoResponse : BaseResponse
{
    public dynamic? Payload { get; }

    private BaseDtoResponse() : base(true, string.Empty)
    {
    }

    public BaseDtoResponse(dynamic payload) : this()
    {
        Payload = payload;
    }

    public BaseDtoResponse(dynamic payload, string message, int code = 0) : base(true, message, code)
    {
        Payload = payload;
    }
}