using Discord.Core.DTOs.MessageRequests.Responses;
using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class DirectMessageRequestService: IDirectMessageRequestService
{
    private readonly AppDbContext _dbContext;

    public DirectMessageRequestService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<DirectMessageRequestResponseDto>>GetIncomingAsync( int userId,  CancellationToken cancellationToken = default)
    {
        return await _dbContext.DirectMessageRequests
            .AsNoTracking()
            .Where(request =>request.RecipientId == userId &&request.Status ==
                    MessageRequestStatus.Pending)
            .OrderByDescending(request =>
                request.CreatedAt)
            .Select(request =>
                new DirectMessageRequestResponseDto
                {
                    Id = request.Id,

                    ConversationId =request.ConversationId,
                    SenderId = request.SenderId,
                    SenderUsername = request.Sender.Username,
                    SenderDisplayName = request.Sender.DisplayName,
                    SenderAvatarUrl =request.Sender.AvatarUrl,
                    Status = request.Status,
                    CreatedAt = request.CreatedAt
                })
            .ToArrayAsync(cancellationToken);
    }

    public async Task AcceptAsync(int requestId,int userId,CancellationToken cancellationToken = default)
    {
        var request = await GetPendingRequestAsync(
            requestId,
            userId,
            cancellationToken);

        request.Status =MessageRequestStatus.Accepted;

        request.RespondedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task IgnoreAsync(
        int requestId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var request = await GetPendingRequestAsync(
            requestId,
            userId,
            cancellationToken);

        request.Status =
            MessageRequestStatus.Ignored;

        request.RespondedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task MarkAsSpamAsync( int requestId, int userId,CancellationToken cancellationToken = default)
    {
        var request = await GetPendingRequestAsync( requestId,userId,
            cancellationToken);

        request.Status = MessageRequestStatus.Spam;

        request.RespondedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Core.Entities.Conversations.DirectMessageRequest>GetPendingRequestAsync(int requestId, int userId,
            CancellationToken cancellationToken)
    {
        if (requestId <= 0)
        {
            throw new BadRequestException( "Mesaj sorğusunun ID-si düzgün deyil.");
        }

        var request =await _dbContext.DirectMessageRequests.FirstOrDefaultAsync( item =>item.Id == requestId && item.RecipientId == userId,cancellationToken)?? throw new KeyNotFoundException(
                "Mesaj sorğusu tapılmadı.");

        if (request.Status !=MessageRequestStatus.Pending)
        {
            throw new ConflictException("Bu mesaj sorğusu artıq cavablandırılıb.");
        }

        return request;
    }
}