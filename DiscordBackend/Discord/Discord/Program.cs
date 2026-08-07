using Discord.Application.Validators.Auth;
using Discord.Authorization;
using Discord.Core.Entities;
using Discord.Core.Interfaces;
using Discord.Extensions;
using Discord.Hubs;
using Discord.Infrastructure.Data;
using Discord.Infrastructure.Services;
using Discord.Middleware;
using Discord.Realtime;
using Discord.Services;
using Discord.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();
builder.Services.AddAuthCompletion(builder.Configuration);

builder.Services.AddJwtAuthentication(
    builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicyNames.PlatformAdmin,
        policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(nameof(Discord.Core.Enums.PlatformRole.Admin))
            .AddRequirements(new PlatformAdminRequirement()));
});
builder.Services.AddSignalR();

builder.Services.AddSingleton<VoiceConnectionTracker>();

builder.Services.AddSingleton<UserConnectionTracker>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserPresenceService,UserPresenceService>();
builder.Services.AddScoped<IFileStorageService,LocalFileStorageService>();
builder.Services.AddScoped<IServerService, ServerService>();
builder.Services.AddScoped<IChannelService, ChannelService>();
builder.Services.AddScoped<IServerInviteService,ServerInviteService>();
builder.Services.AddScoped<IServerMemberService,ServerMemberService>();
builder.Services.AddScoped<IPasswordHasher<User>,PasswordHasher<User>>();
builder.Services.AddScoped<IMessageService,MessageService>();
builder.Services.AddScoped<IChannelAccessService,ChannelAccessService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IConversationAccessService,ConversationAccessService>();
builder.Services.AddScoped<IConversationService,ConversationService>();
builder.Services.AddScoped<IDirectMessageRequestService,DirectMessageRequestService>();
builder.Services.AddScoped<IServerPermissionService,ServerPermissionService>();
builder.Services.AddScoped<IServerRoleService,ServerRoleService>();
builder.Services.AddScoped<IChannelPermissionService, ChannelPermissionService>();
builder.Services.AddScoped<IChannelPermissionOverrideService, ChannelPermissionOverrideService>();
builder.Services.AddScoped<IChannelPermissionRealtimeNotifier,ChannelPermissionRealtimeNotifier>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAuthorizationHandler, PlatformAdminAuthorizationHandler>();

var connectionString =builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Default connection string tapılmadı.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();
using (var scope =
       app.Services.CreateScope())
{
    var userPresenceService =
        scope.ServiceProvider
            .GetRequiredService<
                IUserPresenceService>();

    await userPresenceService
        .ResetAllUsersToOfflineAsync();
}

app.UseMiddleware<ExceptionMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseCors("FrontendPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<VoiceHub>("/hubs/voice");
app.Run();
