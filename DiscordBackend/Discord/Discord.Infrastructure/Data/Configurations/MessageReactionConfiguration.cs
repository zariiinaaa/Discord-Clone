using Discord.Core.Entities.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations;

public class MessageReactionConfiguration: IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(
        EntityTypeBuilder<MessageReaction> builder)
    {
        builder.ToTable("MessageReactions");

        builder.HasKey(reaction =>
            reaction.Id);

        builder.Property(reaction =>
                reaction.Emoji)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(reaction => new
        {
            reaction.MessageId,
            reaction.UserId,
            reaction.Emoji
        })
            .IsUnique();

        builder.HasOne(reaction =>
                reaction.Message)
            .WithMany(message =>
                message.Reactions)
            .HasForeignKey(reaction =>
                reaction.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(reaction =>
                reaction.User)
            .WithMany(user =>
                user.MessageReactions)
            .HasForeignKey(reaction =>
                reaction.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}