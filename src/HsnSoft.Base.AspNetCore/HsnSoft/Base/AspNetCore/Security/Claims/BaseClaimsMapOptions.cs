using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.AspNetCore.Security.Claims;

public class BaseClaimsMapOptions
{
    public Dictionary<string, Func<string>> Maps { get; }

    public BaseClaimsMapOptions()
    {
        Maps = new Dictionary<string, Func<string>>
        {
            { JwtRegisteredClaimNames.Sub, () => BaseClaimTypes.UserId },
            { "role", () => BaseClaimTypes.Role },
            { JwtRegisteredClaimNames.Email, () => BaseClaimTypes.Email },
            { JwtRegisteredClaimNames.UniqueName, () => BaseClaimTypes.UserName },
            { JwtRegisteredClaimNames.GivenName, () => BaseClaimTypes.Name },
            { JwtRegisteredClaimNames.FamilyName, () => BaseClaimTypes.SurName },
        };
    }
}