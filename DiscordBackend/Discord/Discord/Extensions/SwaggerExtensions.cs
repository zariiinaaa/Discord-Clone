using Microsoft.OpenApi;

namespace Discord.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerDocumentation(
        this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Discord Clone API",
                    Version = "v1",
                    Description = "Discord Clone layihəsi üçün ASP.NET Core Web API"
                });

                options.AddSecurityDefinition(
                    "bearer",
                    new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "JWT access token daxil edin."
                    });

                options.AddSecurityRequirement(
                    document => new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(
                            "bearer",
                            document)] = []
                    });
            });

            return services;
        }
    }
}
