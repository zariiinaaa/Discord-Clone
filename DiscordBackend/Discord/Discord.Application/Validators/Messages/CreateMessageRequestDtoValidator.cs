using Discord.Core.DTOs.Messages.Requests;
using FluentValidation;

namespace Discord.Application.Validators.Messages;

public class CreateMessageRequestDtoValidator: AbstractValidator<CreateMessageRequestDto>
{
    public CreateMessageRequestDtoValidator()
    {
        RuleFor(request => request.Content)
            .NotEmpty()
            .WithMessage("Mesaj boş ola bilməz.")
            .MaximumLength(4000)
            .WithMessage(
                "Mesaj maksimum 4000 simvol ola bilər.");

        RuleFor(request => request.ReplyToMessageId)
            .GreaterThan(0)
            .WithMessage(
                "Cavab verilən mesaj ID-si düzgün deyil.")
            .When(request =>
                request.ReplyToMessageId.HasValue);
    }
}