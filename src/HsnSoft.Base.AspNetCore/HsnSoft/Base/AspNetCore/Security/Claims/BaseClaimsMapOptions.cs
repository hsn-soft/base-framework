using System;
using System.Collections.Generic;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.AspNetCore.Security.Claims;

public class BaseClaimsMapOptions
{
    public Dictionary<string, Func<string>> Maps { get; }

    public BaseClaimsMapOptions()
    {
        Maps = new Dictionary<string, Func<string>>
        {
            { "sub", () => BaseClaimTypes.UserId },
            { "role", () => BaseClaimTypes.Role },
            { "email", () => BaseClaimTypes.Email },
            { "name", () => BaseClaimTypes.UserName },
            { "family_name", () => BaseClaimTypes.SurName },
            { "given_name", () => BaseClaimTypes.Name }
        };
    }
}