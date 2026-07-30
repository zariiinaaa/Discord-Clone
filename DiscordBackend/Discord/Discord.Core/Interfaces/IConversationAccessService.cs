using System;
using System.Collections.Generic;
using System.Text;

using Discord.Core.Entities.Conversations;

namespace Discord.Core.Interfaces;

public interface IConversationAccessService
{
    Task<Conversation> GetAccessibleConversationAsync(int conversationId,int userId,
        CancellationToken cancellationToken = default);
}