using Discord.Core.DTOs.Channels.Requests;
using Discord.Core.Enums;
using FluentValidation;

namespace Discord.Application.Validators.Channels;

public class CreateChannelRequestDtoValidator
    : AbstractValidator<CreateChannelRequestDto>
{
    public CreateChannelRequestDtoValidator()
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

        RuleFor(request => request.Topic)
            .Must(topic => string.IsNullOrWhiteSpace(topic))
            .WithMessage(
                "Topic yalnız text channel üçün yazıla bilər.")
            .When(request =>
                request.Type != ChannelType.Text);

        RuleFor(request => request.Type)
            .IsInEnum()
            .WithMessage("Kanal tipi düzgün deyil.");

        RuleFor(request => request.ParentCategoryId)
            .Null()
            .WithMessage(
                "Category başqa category daxilində ola bilməz.")
            .When(request =>
                request.Type == ChannelType.Category);

        RuleFor(request => request.Bitrate)
            .InclusiveBetween(8000, 96000)
            .WithMessage(
                "Bitrate 8000–96000 arasında olmalıdır.")
            .When(request =>
                request.Type == ChannelType.Voice &&
                request.Bitrate.HasValue);

        RuleFor(request => request.UserLimit)
            .InclusiveBetween(0, 99)
            .WithMessage(
                "User limit 0–99 arasında olmalıdır.")
            .When(request =>
                request.Type == ChannelType.Voice &&
                request.UserLimit.HasValue);

        RuleFor(request => request.Bitrate)
            .Null()
            .WithMessage(
                "Bitrate yalnız voice channel üçün yazıla bilər.")
            .When(request =>
                request.Type != ChannelType.Voice);

        RuleFor(request => request.UserLimit)
            .Null()
            .WithMessage(
                "User limit yalnız voice channel üçün yazıla bilər.")
            .When(request =>
                request.Type != ChannelType.Voice);
    }
}