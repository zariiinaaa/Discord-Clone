using Discord.Core.DTOs.Users.Requests;
using Discord.Core.Enums;
using FluentValidation;

namespace Discord.Application.Validators.Users;

public class ChangeStatusRequestDtoValidator: AbstractValidator<ChangeStatusRequestDto>
{
    public ChangeStatusRequestDtoValidator()
    {
        RuleFor(request => request.Status)
            .IsInEnum()
            .WithMessage("Status dəyəri etibarsızdır.")
            .NotEqual(UserStatus.Offline)
            .WithMessage(
                "Offline statusu istifadəçi tərəfindən seçilə bilməz.");
    }
}