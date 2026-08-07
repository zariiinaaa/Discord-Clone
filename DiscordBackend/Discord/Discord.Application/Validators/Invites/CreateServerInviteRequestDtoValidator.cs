using Discord.Core.DTOs.Invites.Requests;
using FluentValidation;

namespace Discord.Application.Validators.Invites;

public class CreateServerInviteRequestDtoValidator
    : AbstractValidator<CreateServerInviteRequestDto>
{
    public CreateServerInviteRequestDtoValidator()
    {
        RuleFor(request => request.ChannelId)
            .GreaterThan(0)
            .WithMessage("Kanal ID-si düzgün olmalıdır.");

        RuleFor(request => request.ExpirationHours)
            .InclusiveBetween(1, 168)
            .WithMessage("Dəvətin müddəti 1–168 saat arasında olmalıdır.")
            .When(request => request.ExpirationHours.HasValue);

        RuleFor(request => request.MaxUses)
            .InclusiveBetween(1, 100)
            .WithMessage("Maksimum istifadə sayı 1–100 arasında olmalıdır.")
            .When(request => request.MaxUses.HasValue);
    }
}