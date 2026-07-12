using Discord.Core.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Discord.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddJwtAuthentication(
       this IServiceCollection services,
       IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("JwtSettings");

            services.Configure<JwtSettings>(jwtSection);

            var jwtSettings = jwtSection.Get<JwtSettings>()
                ?? throw new InvalidOperationException(
                    "JwtSettings tapılmadı.");

            if (string.IsNullOrWhiteSpace(jwtSettings.Secret)
                || jwtSettings.Secret.Length < 32)
            {
                throw new InvalidOperationException(
                    "JWT secret minimum 32 simvol olmalıdır.");
            }

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        JwtBearerDefaults.AuthenticationScheme;

                    options.DefaultChallengeScheme =
                        JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,

                            ValidIssuer = jwtSettings.Issuer,
                            ValidAudience = jwtSettings.Audience,

                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtSettings.Secret)),

                            ClockSkew = TimeSpan.Zero,
                            NameClaimType = ClaimTypes.Name,
                            RoleClaimType = ClaimTypes.Role
                        };
                });

            services.AddAuthorization();

            return services;
        }
    }
}
