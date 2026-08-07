using Discord.Core.DTOs.Admin.Requests;
using FluentValidation;

namespace Discord.Application.Validators.Admin;

public class BanPlatformUserRequestDtoValidator : AbstractValidator<BanPlatformUserRequestDto>
{
    public BanPlatformUserRequestDtoValidator()
    {
        RuleFor(request => request.Reason)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithMessage("Ban səbəbi boş ola bilməz.")
            .MaximumLength(500)
            .WithMessage("Ban səbəbi maksimum 500 simvol ola bilər.");

        RuleFor(request => request.BannedUntil)
            .Must(value => !value.HasValue || value.Value.Kind == DateTimeKind.Utc)
            .WithMessage("Ban bitmə vaxtı UTC formatında olmalıdır.")
            .Must(value => !value.HasValue || value.Value > DateTime.UtcNow)
            .WithMessage("Ban bitmə vaxtı gələcəkdə olmalıdır.");
    }
}
