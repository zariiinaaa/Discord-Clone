using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.ChannelPermissions.Requests;

public class UpdateChannelPermissionOverridesRequestDto
{
    public List<ChannelPermissionOverrideItemRequestDto> Overrides { get; set; }
        = new List<ChannelPermissionOverrideItemRequestDto>();
}
