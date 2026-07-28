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
var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<
    RegisterRequestDtoValidator>();

builder.Services.AddJwtAuthentication(
    builder.Configuration);

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFileStorageService,LocalFileStorageService>();
builder.Services.AddScoped<IServerService, ServerService>();
builder.Services.AddScoped<IChannelService, ChannelService>();
builder.Services.AddScoped<IServerInviteService,ServerInviteService>();
builder.Services.AddScoped<IServerMemberService,ServerMemberService>();
builder.Services.AddScoped<IPasswordHasher<User>,PasswordHasher<User>>();
builder.Services.AddScoped<IMessageService,MessageService>();
var connectionString =builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Default connection string tapılmadı.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();