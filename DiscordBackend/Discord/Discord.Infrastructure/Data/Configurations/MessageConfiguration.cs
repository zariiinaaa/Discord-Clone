using Discord.Core.Entities.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations;

public class MessageConfiguration: IEntityTypeConfiguration<Message>
{
    public void Configure(
        EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Content)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(message => message.IsPinned)
            .HasDefaultValue(false);

        builder.HasIndex(message => new
        {
            message.ChannelId,
            message.CreatedAt
        });

        builder.HasOne(message => message.Channel)
            .WithMany(channel => channel.Messages)
            .HasForeignKey(message => message.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(message => message.Author)
            .WithMany(user => user.SentMessages)
            .HasForeignKey(message => message.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(message => message.ReplyToMessage)
            .WithMany(message => message.Replies)
            .HasForeignKey(message => message.ReplyToMessageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}