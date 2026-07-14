using Discord.Core.DTOs.Channels.Requests;
using FluentValidation;

namespace Discord.Application.Validators.Channels;

public class UpdateChannelRequestDtoValidator
    : AbstractValidator<UpdateChannelRequestDto>
{
    public UpdateChannelRequestDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("Kanal adı boş ola bilməz.")
            .MaximumLength(100)
            .WithMessage(
                "Kanal adı maksimum 100 simvol ola bilər.");

        RuleFor(request => request.Topic)
            .MaximumLength(1024)
            .WithMessage(
                "Topic maksimum 1024 simvol ola bilər.")
            .When(request =>
                !string.IsNullOrWhiteSpace(request.Topic));

        RuleFor(request => request.Bitrate)
            .InclusiveBetween(8000, 96000)
            .WithMessage(
                "Bitrate 8000–96000 arasında olmalıdır.")
            .When(request => request.Bitrate.HasValue);

        RuleFor(request => request.UserLimit)
            .InclusiveBetween(0, 99)
            .WithMessage(
                "User limit 0–99 arasında olmalıdır.")
            .When(request => request.UserLimit.HasValue);
    }
}