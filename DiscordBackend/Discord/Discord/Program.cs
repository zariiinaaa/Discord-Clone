using Discord.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Discord.Application.Validators.Auth;
using FluentValidation;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Services;
using Discord.Extensions;
using Discord.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Discord.Middleware;
using Discord.Services;
using Discord.Services.Interfaces;
using Discord.Hubs;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();

builder.Services.AddJwtAuthentication(
    builder.Configuration);
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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<VoiceHub>("/hubs/voice");
app.Run();