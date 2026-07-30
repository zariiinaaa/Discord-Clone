using Discord.Core.Entities.Conversations;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class ConversationAccessService: IConversationAccessService
{
    private readonly AppDbContext _dbContext;

    public ConversationAccessService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Conversation>GetAccessibleConversationAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
    {
        var conversation =await _dbContext.Conversations
                .AsNoTracking()
                .Include(conversation =>
                    conversation.Members)
                .FirstOrDefaultAsync(
                    conversation =>
                        conversation.Id == conversationId,
                    cancellationToken)
            ?? throw new KeyNotFoundException(
                "Conversation tapılmadı.");

        var isMember = conversation.Members.Any( member => member.UserId == userId);

        if (!isMember)
        {
            throw new ForbiddenException("Bu conversation-a daxil olmaq icazəniz yoxdur.");
        }

        return conversation;
    }
}