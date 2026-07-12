using Discord.Core.DTOs.Auth.Requests;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Application.Validators.Auth
{
    public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestDtoValidator()
        {
            RuleFor(request => request.EmailOrUsername)
                .NotEmpty()
                .WithMessage("Email və ya username boş ola bilməz.")
                .MaximumLength(256);

            RuleFor(request => request.Password)
                .NotEmpty()
                .WithMessage("Password boş ola bilməz.");
        }
    }
}
