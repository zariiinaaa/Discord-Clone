using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Servers.Requests
{
    public class CreateServerRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
    }
}
