using Discord.Core.DTOs.ServerRoles.Requests;
using FluentValidation;

namespace Discord.Application.Validators.ServerRoles;

public class CreateServerRoleRequestDtoValidator: AbstractValidator<CreateServerRoleRequestDto>
{
    public CreateServerRoleRequestDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage(
                "Rol adı boş ola bilməz.")
            .MaximumLength(100)
            .WithMessage(
                "Rol adı maksimum 100 simvol ola bilər.");

        RuleFor(request => request.ColorHex)
            .Matches("^#[0-9A-Fa-f]{6}$")
            .WithMessage(
                "Rol rəngi #RRGGBB formatında olmalıdır.")
            .When(request =>
                !string.IsNullOrWhiteSpace(
                    request.ColorHex));

        RuleFor(request => request.Permissions)
            .Must(permissions =>
                permissions.Distinct().Count() ==
                permissions.Count)
            .WithMessage(
                "Eyni permission iki dəfə göndərilə bilməz.");

        RuleForEach(request => request.Permissions)
            .IsInEnum()
            .WithMessage(
                "Düzgün olmayan permission göndərilib.");
    }
}