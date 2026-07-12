using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Auth.Requests
{
    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
