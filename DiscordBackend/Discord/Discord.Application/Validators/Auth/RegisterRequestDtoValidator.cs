using Discord.Core.DTOs.Auth.Requests;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Application.Validators.Auth
{
    public class RegisterRequestDtoValidator:AbstractValidator<RegisterRequestDto>
    {
        public RegisterRequestDtoValidator()
        {
            RuleFor(request => request.Username)
                .NotEmpty()
                .WithMessage("Username boş ola bilməz.")
                .Length(3, 32)
                .WithMessage("Username 3–32 simvol arasında olmalıdır.")
                .Matches("^[a-zA-Z0-9_]+$")
                .WithMessage("Username yalnız hərf, rəqəm və _ saxlaya bilər.");

            RuleFor(request => request.DisplayName)
                .NotEmpty()
                .WithMessage("Display name boş ola bilməz.")
                .MaximumLength(100)
                .WithMessage("Display name maksimum 100 simvol ola bilər.");

            RuleFor(request => request.Email)
                .NotEmpty()
                .WithMessage("Email boş ola bilməz.")
                .EmailAddress()
                .WithMessage("Düzgün email ünvanı daxil edin.")
                .MaximumLength(256);

            RuleFor(request => request.Password)
                .NotEmpty()
                .WithMessage("Password boş ola bilməz.")
                .MinimumLength(8)
                .WithMessage("Password minimum 8 simvol olmalıdır.")
                .Matches("[A-Z]")
                .WithMessage("Password ən azı bir böyük hərf saxlamalıdır.")
                .Matches("[a-z]")
                .WithMessage("Password ən azı bir kiçik hərf saxlamalıdır.")
                .Matches("[0-9]")
                .WithMessage("Password ən azı bir rəqəm saxlamalıdır.");
        }
    }
}
