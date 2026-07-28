using Discord.Application.Mappings;
using Discord.Core.DTOs.Invites.Requests;
using Discord.Core.DTOs.Invites.Responses;
using Discord.Core.Entities.Servers;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Discord.Infrastructure.Services;

public class ServerInviteService : IServerInviteService
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<CreateServerInviteRequestDto>_createInviteValidator;

    public ServerInviteService(AppDbContext dbContext,IValidator<CreateServerInviteRequestDto>createInviteValidator)
    {
        _dbContext = dbContext;
        _createInviteValidator = createInviteValidator;
    }

    public async Task<ServerInviteResponseDto> CreateAsync(int serverId,int userId,
        CreateServerInviteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationResult =
            await _createInviteValidator.ValidateAsync(request,cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var server = await _dbContext.Servers
            .AsNoTracking()
            .FirstOrDefaultAsync(server => server.Id == serverId,cancellationToken)?? throw new KeyNotFoundException("Server tapılmadı.");

        if (server.OwnerId != userId)
        {
            throw new ForbiddenException("Yalnız server sahibi dəvət yarada bilər.");
        }

        string code;

        do
        {
            code = GenerateInviteCode();
        }
        while (await _dbContext.ServerInvites.AnyAsync(
                invite => invite.Code == code,
                cancellationToken));

        var invite = new ServerInvite
        {
            Code = code,
            ServerId = serverId,
            CreatedByUserId = userId,
            ExpiresAt = request.ExpirationHours.HasValue? DateTime.UtcNow.AddHours(request.ExpirationHours.Value): null,
            MaxUses = request.MaxUses,
            Uses = 0,
            IsRevoked = false
        };

        _dbContext.ServerInvites.Add(invite);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return invite.ToResponseDto(server.Name);
    }

    public async Task JoinAsync(string code,int userId,CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new BadRequestException("Dəvət kodu boş ola bilməz.");
        }

        var normalizedCode = code.Trim().ToLowerInvariant();

        var invite = await _dbContext.ServerInvites
            .FirstOrDefaultAsync(
                invite => invite.Code == normalizedCode,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Dəvət tapılmadı.");

        if (invite.IsRevoked)
        {
            throw new BadRequestException(
                "Bu dəvət ləğv edilib.");
        }

        if (invite.ExpiresAt.HasValue &&
            invite.ExpiresAt.Value <= DateTime.UtcNow)
        {
            throw new BadRequestException(
                "Bu dəvətin istifadə müddəti bitib.");
        }

        if (invite.MaxUses.HasValue &&
            invite.Uses >= invite.MaxUses.Value)
        {
            throw new BadRequestException(
                "Bu dəvətin istifadə limiti bitib.");
        }

        var isAlreadyMember =await _dbContext.ServerMembers.AnyAsync(
                member =>member.ServerId == invite.ServerId &&
                    member.UserId == userId,
                cancellationToken);

        if (isAlreadyMember)
        {
            throw new ConflictException("İstifadəçi artıq bu serverin üzvüdür.");
        }

        var serverMember = new ServerMember
        {
            ServerId = invite.ServerId,
            UserId = userId,
            IsMuted = false,
            IsDeafened = false
        };

        _dbContext.ServerMembers.Add(serverMember);

        invite.Uses++;
        invite.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateInviteCode()
    {
        var randomBytes =RandomNumberGenerator.GetBytes(8);

        return Convert.ToHexString(randomBytes).ToLowerInvariant();
    }
}