using Discord.Core.DTOs.Auth.Requests;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Application.Validators.Auth
{
    public class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
    {
        public RefreshTokenRequestDtoValidator()
        {
            RuleFor(request => request.RefreshToken)
                .NotEmpty()
                .WithMessage("Refresh token boş ola bilməz.");
        }

    }
}
