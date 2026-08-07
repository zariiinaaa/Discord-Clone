using System;
using System.Collections.Generic;
using System.Text;

using Discord.Core.Enums;

namespace Discord.Core.DTOs.ChannelPermissions.Responses;

public class ChannelRolePermissionOverrideResponseDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public ServerPermission Permission { get; set; }
    public PermissionOverrideType OverrideType { get; set; }
}