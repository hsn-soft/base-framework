using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Serilog.Mask;

namespace Hhs.AuthServer.Models;

public class RegistrationRequest
{
    [Required]
    public string Email { get; set; } = null!;

    [Required]
    public string Username { get; set; } = null!;

    [Required]
    [SensitiveData]
    public string Password { get; set; } = null!;
}