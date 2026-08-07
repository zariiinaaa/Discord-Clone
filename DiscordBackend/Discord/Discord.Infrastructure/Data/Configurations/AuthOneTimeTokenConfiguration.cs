using Discord.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations;

public class AuthOneTimeTokenConfiguration : IEntityTypeConfiguration<AuthOneTimeToken>
{
    public void Configure(EntityTypeBuilder<AuthOneTimeToken> builder)
    {
        builder.ToTable("AuthOneTimeTokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Purpose).HasConversion<int>();
        builder.Property(token => token.TokenHash).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.Property(token => token.CreatedByIp).HasMaxLength(45);
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.UserId, token.Purpose, token.CreatedAt });
        builder.HasOne(token => token.User)
            .WithMany(user => user.AuthOneTimeTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
