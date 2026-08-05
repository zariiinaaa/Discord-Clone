using Discord.Core.Entities.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations;

public class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(
        EntityTypeBuilder<MessageAttachment> builder)
    {
        builder.ToTable(
            "MessageAttachments",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_MessageAttachments_FileSize",
                    "[FileSize] > 0");
            });

        builder.HasKey(attachment =>
            attachment.Id);

        builder.Property(attachment =>
                attachment.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(attachment =>
                attachment.StoredFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(attachment =>
                attachment.FileUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(attachment =>
                attachment.ContentType)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(attachment =>
                attachment.FileSize)
            .IsRequired();

        builder.HasIndex(attachment =>
            attachment.MessageId);

        builder.HasOne(attachment =>
                attachment.Message)
            .WithMany(message =>
                message.Attachments)
            .HasForeignKey(attachment =>
                attachment.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}