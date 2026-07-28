using Discord.Core.DTOs.Messages.Requests;
using FluentValidation;

namespace Discord.Application.Validators.Messages;

public class UpdateMessageRequestDtoValidator: AbstractValidator<UpdateMessageRequestDto>
{
    public UpdateMessageRequestDtoValidator()
    {
        RuleFor(request => request.Content)
            .NotEmpty()
            .WithMessage("Mesaj boş ola bilməz.")
            .MaximumLength(4000)
            .WithMessage(
                "Mesaj maksimum 4000 simvol ola bilər.");
    }
}