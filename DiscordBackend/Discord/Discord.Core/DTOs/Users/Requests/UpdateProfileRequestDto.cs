using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Users.Requests
{
    public class UpdateProfileRequestDto
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? Bio { get; set; }
    }
}
