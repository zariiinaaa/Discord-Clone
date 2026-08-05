using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Messages.Responses;

public class MessageAttachmentResponseDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
}
