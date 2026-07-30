using Discord.Core.DTOs.Conversations.Requests;
using FluentValidation;

namespace Discord.Application.Validators.Conversations;

public class AddGroupMemberRequestDtoValidator : AbstractValidator<AddGroupMemberRequestDto>
{
    public AddGroupMemberRequestDtoValidator()
    {
        RuleFor(request => request.UserId).GreaterThan(0)
            .WithMessage(
                "İstifadəçi ID-si düzgün deyil.");
    }
}