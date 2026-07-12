using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Auth.Requests
{
    public class LoginRequestDto
    {
        public string EmailOrUsername { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
