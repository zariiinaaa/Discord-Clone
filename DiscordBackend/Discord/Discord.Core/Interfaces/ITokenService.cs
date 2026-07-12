using Discord.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        string HashToken(string token);
        DateTime GetAccessTokenExpiration();
        DateTime GetRefreshTokenExpiration();
    }
}
