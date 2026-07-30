using Discord.Core.Entities.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations;

public class ConversationMemberConfiguration: IEntityTypeConfiguration<ConversationMember>
{
    public void Configure( EntityTypeBuilder<ConversationMember> builder)
    {
        builder.ToTable("ConversationMembers");

        builder.HasKey(member => member.Id);

        builder.Property(member => member.IsMuted).HasDefaultValue(false);

        builder.HasIndex(member => new
        {
            member.ConversationId,
            member.UserId
        })
            .IsUnique();

        builder.HasOne(member =>member.Conversation)
            .WithMany(conversation =>
                conversation.Members)
            .HasForeignKey(member =>
                member.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(member =>member.User)
            .WithMany(user =>
                user.ConversationMemberships)
            .HasForeignKey(member =>
                member.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}