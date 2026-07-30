using Discord.Core.Entities.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations;

public class DirectMessageRequestConfiguration: IEntityTypeConfiguration<DirectMessageRequest>
{
    public void Configure(EntityTypeBuilder<DirectMessageRequest> builder)
    {
        builder.ToTable(
            "DirectMessageRequests",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_DirectMessageRequests_DifferentUsers",
                    "[SenderId] <> [RecipientId]");
            });

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Status)
            .IsRequired();

        builder.HasIndex(request => new
        {
            request.ConversationId,
            request.RecipientId
        }).IsUnique();

        builder.HasIndex(request => new
        {
            request.RecipientId,
            request.Status
        });

        builder.HasOne(request => request.Conversation)
            .WithMany(conversation =>
                conversation.MessageRequests)
            .HasForeignKey(request =>
                request.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(request => request.Sender)
            .WithMany(user =>
                user.SentDirectMessageRequests)
            .HasForeignKey(request => request.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.Recipient)
            .WithMany(user =>
                user.ReceivedDirectMessageRequests)
            .HasForeignKey(request =>
                request.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}