using Discord.Core.DTOs.Auth.Requests;
using FluentValidation;

namespace Discord.Application.Validators.Auth;

public class VerifyEmailRequestDtoValidator : AbstractValidator<VerifyEmailRequestDto>
{
    public VerifyEmailRequestDtoValidator()
    {
        RuleFor(request => request.Token).NotEmpty().MaximumLength(500);
    }
}
