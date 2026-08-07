using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.ServerMembers.Requests;

public class BanServerMemberRequestDto
{
    public string? Reason { get; set; }
}