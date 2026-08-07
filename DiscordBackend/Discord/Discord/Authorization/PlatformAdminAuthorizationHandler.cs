using Discord.Core.Enums;
using Discord.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Discord.Authorization;

public sealed class PlatformAdminAuthorizationHandler
    : AuthorizationHandler<PlatformAdminRequirement>
{
    private readonly AppDbContext _dbContext;

    public PlatformAdminAuthorizationHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformAdminRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return;
        }

        if (!context.User.IsInRole(nameof(PlatformRole.Admin)))
        {
            return;
        }

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var isAuthorized = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user =>
                user.Id == userId &&
                user.Role == PlatformRole.Admin &&
                (!user.IsBanned ||
                    (user.BannedUntil.HasValue && user.BannedUntil <= now)));

        if (isAuthorized)
        {
            context.Succeed(requirement);
        }
    }
}
