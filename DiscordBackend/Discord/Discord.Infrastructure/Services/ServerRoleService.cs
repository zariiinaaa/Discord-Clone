using Discord.Application.Mappings;
using Discord.Core.DTOs.ServerRoles.Requests;
using Discord.Core.DTOs.ServerRoles.Responses;
using Discord.Core.Entities.Servers;
using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class ServerRoleService : IServerRoleService
{
    private readonly AppDbContext _dbContext;
    private readonly IServerPermissionService _serverPermissionService;
    private readonly IValidator<CreateServerRoleRequestDto> _createRoleValidator;

    private readonly IValidator<UpdateServerRoleRequestDto> _updateRoleValidator;

    public ServerRoleService(AppDbContext dbContext,IServerPermissionService serverPermissionService,
    IValidator<CreateServerRoleRequestDto>createRoleValidator, IValidator<UpdateServerRoleRequestDto>
     updateRoleValidator)
    {
        _dbContext = dbContext;
        _serverPermissionService =serverPermissionService;
        _createRoleValidator =createRoleValidator;
        _updateRoleValidator =updateRoleValidator;
    }

    public async Task< IReadOnlyCollection<ServerRoleResponseDto>> GetRolesAsync(int serverId,int userId,
       CancellationToken cancellationToken = default)
    {
        await EnsureServerMemberAsync(serverId,userId,
            cancellationToken);

        var roles = await _dbContext.ServerRoles.AsNoTracking().Include(role =>role.Permissions)
            .Where(role =>role.ServerId == serverId)
            .OrderByDescending(role =>role.Position)
            .ThenBy(role =>role.Name)
            .ToListAsync(cancellationToken);

        return roles.Select(role => role.ToResponseDto()).ToList();
    }

    public async Task<ServerRoleResponseDto>CreateRoleAsync( int serverId,int userId,CreateServerRoleRequestDto request,
      CancellationToken cancellationToken = default)
    {
        var validationResult =await _createRoleValidator.ValidateAsync(request,
        cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        await _serverPermissionService .EnsurePermissionAsync(
         serverId,userId, ServerPermission.ManageRoles,cancellationToken);

        await EnsureCanGrantPermissionsAsync( serverId, userId,request.Permissions,cancellationToken);

        await EnsureCanCreateRoleAsync( serverId, userId, cancellationToken);

        await using var transaction = await _dbContext.Database
        .BeginTransactionAsync(cancellationToken);

        var existingRoles =
            await _dbContext.ServerRoles.Where(role =>
                    role.ServerId == serverId &&
                    !role.IsDefault)
                .ToListAsync(cancellationToken);

        foreach (var existingRole in existingRoles)
        {
            existingRole.Position++;
            existingRole.UpdatedAt =
                DateTime.UtcNow;
        }

        var role = new ServerRole
        {
            Name = request.Name.Trim(),

            ColorHex =string.IsNullOrWhiteSpace(request.ColorHex)? null
             : request.ColorHex.Trim().ToUpperInvariant(),

            Position = 1,
            IsDefault = false,
            IsDisplayedSeparately =request.IsDisplayedSeparately,
            IsMentionable = request.IsMentionable,
            ServerId = serverId
        };

        foreach (var permission in request.Permissions.Distinct())
        {
            role.Permissions.Add(
                new ServerRolePermission
                {
                    Permission = permission
                });
        }

        _dbContext.ServerRoles.Add(role);

        await _dbContext.SaveChangesAsync( cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return role.ToResponseDto();
    }

    public async Task<ServerRoleResponseDto>UpdateRoleAsync(int serverId,int roleId,int userId,UpdateServerRoleRequestDto request,
            CancellationToken cancellationToken = default)
    {
        var validationResult =await _updateRoleValidator.ValidateAsync(
        request,cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException( validationResult.Errors);
        }

        await _serverPermissionService.EnsurePermissionAsync(serverId,userId,ServerPermission.ManageRoles,
          cancellationToken);

        var role = await _dbContext.ServerRoles.Include(currentRole => currentRole.Permissions)
            .FirstOrDefaultAsync(currentRole =>currentRole.Id == roleId && currentRole.ServerId == serverId,
                cancellationToken)?? throw new KeyNotFoundException( "Rol tapılmadı.");

        await EnsureCanManageRoleAsync(serverId,userId,role.Position,cancellationToken);

        await EnsureCanGrantPermissionsAsync(serverId,userId,request.Permissions,
            cancellationToken);

        if (role.IsDefault)
        {
            var isDefaultRoleDataValid =request.Name.Trim() =="@everyone" &&
                string.IsNullOrWhiteSpace(request.ColorHex) &&
                !request.IsDisplayedSeparately &&!request.IsMentionable;

            if (!isDefaultRoleDataValid)
            {
                throw new BadRequestException(
                    "@everyone rolunun adı, rəngi və görünüş parametrləri dəyişdirilə bilməz.");
            }
        }
        else
        {
            role.Name = request.Name.Trim();

            role.ColorHex =string.IsNullOrWhiteSpace(request.ColorHex)? null: request.ColorHex
            .Trim().ToUpperInvariant();

            role.IsDisplayedSeparately =request.IsDisplayedSeparately;

            role.IsMentionable = request.IsMentionable;
        }

        _dbContext.ServerRolePermissions.RemoveRange(role.Permissions);

        role.Permissions.Clear();

        foreach (var permission in request.Permissions.Distinct())
        {
            role.Permissions.Add(
                new ServerRolePermission
                {
                    Permission = permission
                });
        }

        role.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return role.ToResponseDto();
    }

    public async Task DeleteRoleAsync(int serverId, int roleId, int userId,CancellationToken cancellationToken = default)
    {
        await _serverPermissionService.EnsurePermissionAsync(
            serverId, userId, ServerPermission.ManageRoles, cancellationToken);

        var role = await _dbContext.ServerRoles
            .Include(role => role.Permissions)
            .Include(role => role.MemberRoles)
            .FirstOrDefaultAsync(currentRole =>
                currentRole.Id == roleId && currentRole.ServerId == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Rol tapılmadı.");

        if (role.IsDefault)
        {
            throw new BadRequestException("@everyone rolu silinə bilməz.");
        }

        await EnsureCanManageRoleAsync(
            serverId, userId, role.Position, cancellationToken);

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        var channelOverrides = await _dbContext.ChannelRolePermissionOverrides
            .Where(overrideItem => overrideItem.RoleId == roleId)
            .ToListAsync(cancellationToken);

        if (channelOverrides.Count > 0)
        {
            _dbContext.ChannelRolePermissionOverrides.RemoveRange(
                channelOverrides);
        }

        if (role.MemberRoles.Count > 0)
        {
            _dbContext.ServerMemberRoles.RemoveRange(
                role.MemberRoles);
        }

        if (role.Permissions.Count > 0)
        {
            _dbContext.ServerRolePermissions.RemoveRange(
                role.Permissions);
        }

        var higherRoles = await _dbContext.ServerRoles
            .Where(currentRole =>
                currentRole.ServerId == serverId &&
                currentRole.Position > role.Position)
            .ToListAsync(cancellationToken);

        foreach (var higherRole in higherRoles)
        {
            higherRole.Position--;
            higherRole.UpdatedAt = DateTime.UtcNow;
        }

        _dbContext.ServerRoles.Remove(role);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AssignRoleAsync(int serverId, int memberUserId,int roleId, int userId,
      CancellationToken cancellationToken = default)
    {
        await _serverPermissionService.EnsurePermissionAsync(serverId,userId,
        ServerPermission.ManageRoles, cancellationToken);

        var role = await GetRoleAsync( serverId,roleId,cancellationToken);

        if (role.IsDefault)
        {
            throw new BadRequestException("@everyone rolu bütün üzvlərə avtomatik tətbiq olunur.");
        }

        var member = await GetServerMemberAsync(serverId,memberUserId,cancellationToken);

        await EnsureCanManageRoleAsync(serverId,userId, role.Position,cancellationToken);

        await EnsureCanManageMemberAsync(serverId,userId,memberUserId, cancellationToken);

        var roleAlreadyAssigned =await _dbContext.ServerMemberRoles.AnyAsync(memberRole =>
                        memberRole.ServerMemberId ==member.Id &&
                        memberRole.ServerRoleId ==roleId,cancellationToken);

        if (roleAlreadyAssigned)
        {
            return;
        }
        var memberRole = new ServerMemberRole
        {
            ServerMemberId = member.Id,
            ServerRoleId = roleId
        };

        _dbContext.ServerMemberRoles.Add(memberRole);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveRoleAsync(int serverId,int memberUserId,int roleId,int userId,CancellationToken cancellationToken = default)
    {
        await _serverPermissionService.EnsurePermissionAsync(serverId,userId,ServerPermission.ManageRoles,
        cancellationToken);

        var role = await GetRoleAsync(serverId,roleId,cancellationToken);

        if (role.IsDefault)
        {
            throw new BadRequestException("@everyone rolu üzvdən götürülə bilməz.");
        }

        var member = await GetServerMemberAsync(serverId,memberUserId,cancellationToken);

        await EnsureCanManageRoleAsync( serverId,userId,role.Position,cancellationToken);

        await EnsureCanManageMemberAsync(serverId, userId, memberUserId,cancellationToken);

        var memberRole =await _dbContext.ServerMemberRoles.FirstOrDefaultAsync(
         currentMemberRole =>currentMemberRole.ServerMemberId == member.Id &&
         currentMemberRole.ServerRoleId == roleId,cancellationToken);

        if (memberRole is null)
        {
            return;
        }
        _dbContext.ServerMemberRoles.Remove(memberRole);
        await _dbContext.SaveChangesAsync( cancellationToken);
    }

    private async Task EnsureServerMemberAsync(int serverId,int userId,CancellationToken cancellationToken)
    {
        var server = await _dbContext.Servers.AsNoTracking().FirstOrDefaultAsync( currentServer =>
         currentServer.Id == serverId,cancellationToken)?? throw new KeyNotFoundException("Server tapılmadı.");

        if (server.OwnerId == userId)
        {
            return;
        }

        var isMember =await _dbContext.ServerMembers.AsNoTracking().AnyAsync(
         member => member.ServerId == serverId &&member.UserId == userId,
                    cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenException(
                "Yalnız server üzvləri rolları görə bilər.");
        }
    }

    private async Task EnsureCanCreateRoleAsync(int serverId,int userId,CancellationToken cancellationToken)
    {
        var server = await _dbContext.Servers
            .AsNoTracking()
            .FirstAsync(currentServer => currentServer.Id == serverId,cancellationToken);

        if (server.OwnerId == userId)
        {
            return;
        }

        var highestRolePosition =await GetHighestRolePositionAsync( serverId,
        userId,cancellationToken);

        if (highestRolePosition <= 0)
        {
            throw new ForbiddenException(
                "Yeni rol yaratmaq üçün @everyone-dan yuxarı rolunuz olmalıdır.");
        }
    }

    private async Task EnsureCanManageRoleAsync(int serverId,int userId,int rolePosition,CancellationToken cancellationToken)
    {
        var server = await _dbContext.Servers
            .AsNoTracking()
            .FirstAsync(
                currentServer =>
                    currentServer.Id == serverId,
                cancellationToken);

        if (server.OwnerId == userId)
        {
            return;
        }

        var highestRolePosition =await GetHighestRolePositionAsync(serverId,userId,cancellationToken);

        if (highestRolePosition <= rolePosition)
        {
            throw new ForbiddenException("Yalnız öz ən yüksək rolunuzdan aşağı rolları idarə edə bilərsiniz.");
        }
    }

    private async Task EnsureCanManageMemberAsync(int serverId,int userId,int memberUserId,CancellationToken cancellationToken)
    {
        var server = await _dbContext.Servers.AsNoTracking().FirstAsync(currentServer =>currentServer.Id == serverId,
                cancellationToken);

        if (server.OwnerId == userId)
        {
            return;
        }

        if (server.OwnerId == memberUserId)
        {
            throw new ForbiddenException(
                "Server sahibinin rolları dəyişdirilə bilməz.");
        }

        if (userId == memberUserId)
        {
            throw new ForbiddenException(
                "Öz rollarınızı dəyişdirə bilməzsiniz.");
        }

        var currentUserHighestRolePosition = await GetHighestRolePositionAsync(serverId, userId, cancellationToken);

        var targetUserHighestRolePosition =await GetHighestRolePositionAsync(serverId,memberUserId,cancellationToken);

        if (currentUserHighestRolePosition <= targetUserHighestRolePosition)
        {
            throw new ForbiddenException("Eyni və ya daha yüksək rola sahib üzvün rollarını dəyişdirə bilməzsiniz.");
        }
    }

    private async Task EnsureCanGrantPermissionsAsync(int serverId,int userId,IEnumerable<ServerPermission> permissions,
        CancellationToken cancellationToken)
    {
        var server = await _dbContext.Servers.AsNoTracking().FirstAsync(currentServer =>
         currentServer.Id == serverId,cancellationToken);

        if (server.OwnerId == userId)
        {
            return;
        }

        var currentUserPermissions =await _serverPermissionService.GetPermissionsAsync( serverId, userId,
         cancellationToken);

        if (currentUserPermissions.Contains(
                ServerPermission.Administrator))
        {
            return;
        }

        var hasUnavailablePermission = permissions
    .Distinct()
    .Any(permission =>
        !currentUserPermissions.Contains(permission));

        if (hasUnavailablePermission)
        {
            throw new ForbiddenException(
                "Sahib olmadığınız permission-u başqa rola verə bilməzsiniz.");
        }
    }

    private async Task<int>GetHighestRolePositionAsync( int serverId,int userId,CancellationToken cancellationToken)
    {
        var highestRolePosition =await _dbContext.ServerMemberRoles.AsNoTracking().Where(memberRole => memberRole.ServerMember.ServerId ==
                        serverId &&memberRole.ServerMember.UserId ==
                        userId) .MaxAsync( memberRole =>(int?)memberRole .ServerRole.Position,cancellationToken);

        return highestRolePosition ?? 0;
    }

    private async Task<ServerRole> GetRoleAsync(int serverId, int roleId,CancellationToken cancellationToken)
    {
        return await _dbContext.ServerRoles .FirstOrDefaultAsync( role => role.Id == roleId &&role.ServerId == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Rol tapılmadı.");
    }

    private async Task<ServerMember>GetServerMemberAsync(int serverId, int memberUserId,CancellationToken cancellationToken)
    {
        return await _dbContext.ServerMembers.FirstOrDefaultAsync(member => member.ServerId == serverId &&
        member.UserId == memberUserId, cancellationToken)?? throw new KeyNotFoundException(
        "Server üzvü tapılmadı.");
    }
}