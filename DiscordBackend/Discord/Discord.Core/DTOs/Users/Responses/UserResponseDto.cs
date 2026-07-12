using Discord.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Users.Responses
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public UserStatus Status { get; set; }
        public PlatformRole Role { get; set; }
    }
}
