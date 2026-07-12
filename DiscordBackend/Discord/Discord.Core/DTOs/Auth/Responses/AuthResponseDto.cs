using Discord.Core.DTOs.Users.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Auth.Responses
{
    public class AuthResponseDto
    {
        public string TokenType { get; set; } = "Bearer";
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserResponseDto User { get; set; } = null!;
    }
}
