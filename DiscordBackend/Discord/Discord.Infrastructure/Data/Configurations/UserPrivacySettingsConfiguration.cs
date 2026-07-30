using Discord.Core.Entities.Privacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations;

public class UserPrivacySettingsConfiguration: IEntityTypeConfiguration<UserPrivacySettings>
{
    public void Configure(EntityTypeBuilder<UserPrivacySettings> builder)
    {
        builder.ToTable("UserPrivacySettings");

        builder.HasKey(settings => settings.Id);

        builder.HasIndex(settings => settings.UserId).IsUnique();

        builder.Property(settings =>
                settings.AllowDirectMessagesFromServerMembers)
            .HasDefaultValue(true);

        builder.Property(settings =>
                settings.EnableMessageRequests)
            .HasDefaultValue(true);

        builder.HasOne(settings => settings.User)
            .WithOne(user => user.PrivacySettings)
            .HasForeignKey<UserPrivacySettings>(
                settings => settings.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}