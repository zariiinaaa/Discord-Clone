using Discord.Core.Entities.Conversations;
using Discord.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations;

public class ConversationConfiguration: IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable(
            "Conversations",tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Conversations_TypeOwner",
                    "([Type] = 0 AND [OwnerId] IS NULL) " +
                    "OR ([Type] = 1 AND [OwnerId] IS NOT NULL)");
            });

        builder.HasKey(conversation =>
            conversation.Id);

        builder.Property(conversation =>
                conversation.Type)
            .IsRequired();

        builder.Property(conversation =>
                conversation.Name)
            .HasMaxLength(100);

        builder.Property(conversation =>
                conversation.IconUrl)
            .HasMaxLength(500);

        builder.HasIndex(conversation =>
            conversation.Type);

        builder.HasOne(conversation =>
                conversation.Owner)
            .WithMany(user =>
                user.OwnedConversations)
            .HasForeignKey(conversation =>
                conversation.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}