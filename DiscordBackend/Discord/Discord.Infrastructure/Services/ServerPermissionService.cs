using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class ServerPermissionService: IServerPermissionService
{
    private readonly AppDbContext _dbContext;

    public ServerPermissionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ServerPermission>>GetPermissionsAsync(
     int serverId,int userId,CancellationToken cancellationToken = default)
    {
        var server = await _dbContext.Servers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                currentServer =>
                    currentServer.Id == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Server tapılmadı.");

        if (server.OwnerId == userId)
        {
            return Enum.GetValues<ServerPermission>();
        }

        var serverMember =await _dbContext.ServerMembers
        .AsNoTracking().FirstOrDefaultAsync(
          member =>member.ServerId == serverId &&
           member.UserId == userId,
                    cancellationToken)
            ?? throw new ForbiddenException(
                "Yalnız server üzvləri bu əməliyyatı edə bilər.");

        var permissions =
            await _dbContext.ServerRolePermissions
                .AsNoTracking()
                .Where(rolePermission =>rolePermission.ServerRole.ServerId ==serverId &&

                    (
                        rolePermission.ServerRole.IsDefault ||rolePermission.ServerRole.MemberRoles.Any(memberRole =>
      memberRole.ServerMemberId ==serverMember.Id))).Select(rolePermission =>rolePermission.Permission)
       .Distinct().ToListAsync(cancellationToken);

        return permissions;
    }

    public async Task<bool> HasPermissionAsync(int serverId, int userId,ServerPermission permission,CancellationToken cancellationToken = default)
    {
        var permissions =await GetPermissionsAsync(serverId, userId,cancellationToken);

        if (permissions.Contains(
                ServerPermission.Administrator))
        {
            return true;
        }

        return permissions.Contains(permission);
    }

    public async Task EnsurePermissionAsync(int serverId, int userId,ServerPermission permission,
     CancellationToken cancellationToken = default)
    {
        var hasPermission = await HasPermissionAsync(
        serverId,userId, permission,
         cancellationToken);

        if (!hasPermission)
        {
            throw new ForbiddenException(
                "Bu əməliyyatı etmək üçün lazımi icazəniz yoxdur.");
        }
    }
}