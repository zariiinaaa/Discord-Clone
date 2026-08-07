using Discord.Core.DTOs.ServerRoles.Responses;
using Discord.Core.Entities.Servers;

namespace Discord.Application.Mappings;

public static class ServerRoleMappings
{
    public static ServerRoleResponseDto ToResponseDto(this ServerRole role)
    {
        return new ServerRoleResponseDto
        {
            Id = role.Id,
            Name = role.Name,
            ColorHex = role.ColorHex,
            Position = role.Position,
            IsDefault = role.IsDefault,
            IsDisplayedSeparately =
                role.IsDisplayedSeparately,
            IsMentionable = role.IsMentionable,
            ServerId = role.ServerId,

            Permissions = role.Permissions
                .Select(rolePermission =>
                    rolePermission.Permission)
                .Distinct()
                .OrderBy(permission =>
                    (int)permission)
                .ToArray()
        };
    }
}