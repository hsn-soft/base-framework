using System.Collections.Generic;
using System.Text.Json;

namespace HsnSoft.Base.ExceptionHandling.Dtos;

public sealed class BaseErrorResponse : BaseResponse
{
    private BaseErrorResponse() : base(false, string.Empty)
    {
    }
    
    public BaseErrorResponse(string message, int code = 0) : base(false, message, code)
    {
    }

    public BaseErrorResponse(IEnumerable<string> messages, int code = 0) : base(false, messages, code)
    {
    }
    
    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}