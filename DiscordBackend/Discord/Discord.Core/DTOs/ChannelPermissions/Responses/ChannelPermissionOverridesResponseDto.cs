namespace Discord.Core.DTOs.ChannelPermissions.Responses;

public class ChannelPermissionOverridesResponseDto
{
    public int ChannelId { get; set; }

    public IReadOnlyCollection<ChannelRolePermissionOverrideResponseDto>RoleOverrides{ get; set; }
        = new List<ChannelRolePermissionOverrideResponseDto>();

    public IReadOnlyCollection<ChannelMemberPermissionOverrideResponseDto>MemberOverrides{ get; set; }
        = new List<ChannelMemberPermissionOverrideResponseDto>();
}