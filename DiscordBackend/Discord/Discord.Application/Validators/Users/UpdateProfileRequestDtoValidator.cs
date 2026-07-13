using Discord.Core.DTOs.Users.Requests;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Application.Validators.Users
{
    public class UpdateProfileRequestDtoValidator : AbstractValidator<UpdateProfileRequestDto>
    {
        public UpdateProfileRequestDtoValidator()
        {
            RuleFor(request => request.DisplayName)
                .NotEmpty()
                .WithMessage("Display name boş ola bilməz.")
                .MaximumLength(100)
                .WithMessage("Display name maksimum 100 simvol ola bilər.");

            RuleFor(request => request.Bio)
                .MaximumLength(500)
                .WithMessage("Bio maksimum 500 simvol ola bilər.")
                .When(request => !string.IsNullOrWhiteSpace(request.Bio));
        }
    }
}
