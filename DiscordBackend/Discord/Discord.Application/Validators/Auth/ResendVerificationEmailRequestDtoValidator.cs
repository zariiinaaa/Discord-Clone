using Discord.Core.DTOs.Auth.Requests;
using FluentValidation;

namespace Discord.Application.Validators.Auth;

public class ResendVerificationEmailRequestDtoValidator : AbstractValidator<ResendVerificationEmailRequestDto>
{
    public ResendVerificationEmailRequestDtoValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
