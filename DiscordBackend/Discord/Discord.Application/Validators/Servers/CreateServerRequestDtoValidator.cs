using Discord.Core.DTOs.Servers.Requests;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Application.Validators.Servers
{
    public class CreateServerRequestDtoValidator : AbstractValidator<CreateServerRequestDto>
    {
        public CreateServerRequestDtoValidator()
        {
            RuleFor(request => request.Name)
                .NotEmpty()
                .WithMessage("Server adı boş ola bilməz.")
                .MinimumLength(2)
                .WithMessage("Server adı minimum 2 simvol olmalıdır.")
                .MaximumLength(100)
                .WithMessage("Server adı maksimum 100 simvol ola bilər.");

            RuleFor(request => request.Description)
                .MaximumLength(500)
                .WithMessage("Açıqlama maksimum 500 simvol ola bilər.")
                .When(request =>
                    !string.IsNullOrWhiteSpace(request.Description));
        }
    }
}
