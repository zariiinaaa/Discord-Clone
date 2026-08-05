using System;
using System.Collections.Generic;
using System.Text;

using Discord.Core.Enums;
namespace Discord.Core.DTOs.Users.Requests;
public class ChangeStatusRequestDto
{
    public UserStatus Status { get; set; }
}