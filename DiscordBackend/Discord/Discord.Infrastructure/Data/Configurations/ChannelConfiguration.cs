using Discord.Core.Entities.Servers;
using Discord.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations
{
    public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
    {

        public void Configure(
       EntityTypeBuilder<Channel> builder)
        {
            builder.ToTable("Channels");

            builder.HasKey(channel => channel.Id);

            builder.Property(channel => channel.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(channel => channel.Topic)
                .HasMaxLength(1024);

            builder.Property(channel => channel.Type)
                .HasConversion<int>()
                .HasDefaultValue(ChannelType.Text);

            builder.Property(channel => channel.Position)
                .HasDefaultValue(0);

            builder.Property(channel => channel.IsPrivate)
                .HasDefaultValue(false);
            builder.Property(channel => channel.IsPermissionSynced)
    .HasDefaultValue(false);
            builder.HasIndex(channel => channel.ServerId);

            builder.HasIndex(channel => new
            {
                channel.ServerId,
                channel.ParentCategoryId,
                channel.Position
            });

            builder.HasOne(channel => channel.Server)
                .WithMany(server => server.Channels)
                .HasForeignKey(channel => channel.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(channel => channel.ParentCategory)
                .WithMany(channel => channel.ChildChannels)
                .HasForeignKey(channel => channel.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
