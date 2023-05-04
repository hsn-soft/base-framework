using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HsnSoft.Base.ExceptionHandling.Dtos;

public class ErrorResultDto
{
    public string Code { get; }
    public List<string> Messages { get; }

    public ErrorResultDto(IReadOnlyCollection<string> messages, string code = null)
    {
        Code = code ?? string.Empty;
        Messages = messages == null ? new List<string>() : messages.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }

    public ErrorResultDto(string message, string code = null)
    {
        Code = code ?? string.Empty;
        Messages = new List<string>();

        if (!string.IsNullOrWhiteSpace(message))
        {
            Messages.Add(message);
        }
    }
    
    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}