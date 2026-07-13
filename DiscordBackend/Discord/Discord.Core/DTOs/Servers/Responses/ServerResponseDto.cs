using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Servers.Responses
{
    public class ServerResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public string? BannerUrl { get; set; }
        public bool IsPublic { get; set; }
        public int OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
