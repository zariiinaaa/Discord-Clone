using Discord.Core.Entities.Servers;
using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class ChannelAccessService : IChannelAccessService
{
    private readonly AppDbContext _dbContext;

    public ChannelAccessService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Channel>
        GetAccessibleTextChannelAsync(int channelId, int userId,
            CancellationToken cancellationToken = default)
    {
        var channel = await _dbContext.Channels
            .AsNoTracking()
            .Include(channel => channel.Server)
            .FirstOrDefaultAsync(
                channel => channel.Id == channelId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Kanal tapılmadı.");

        if (channel.Type != ChannelType.Text)
        {
            throw new BadRequestException("Bu əməliyyat yalnız text kanal üçün mümkündür.");
        }

        var isMember =
            await _dbContext.ServerMembers.AnyAsync(
                member =>
                    member.ServerId == channel.ServerId &&
                    member.UserId == userId,
                cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenException("Yalnız server üzvləri kanala daxil ola bilər.");
        }

        return channel;
    }
}