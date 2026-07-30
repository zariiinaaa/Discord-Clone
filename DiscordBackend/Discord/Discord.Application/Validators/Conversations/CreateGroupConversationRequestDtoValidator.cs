using Discord.Core.DTOs.Conversations.Requests;
using FluentValidation;

namespace Discord.Application.Validators.Conversations;

public class CreateGroupConversationRequestDtoValidator: AbstractValidator<CreateGroupConversationRequestDto>
{
    public CreateGroupConversationRequestDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage(
                "Group adı boş ola bilməz.")
            .MaximumLength(100)
            .WithMessage(
                "Group adı maksimum 100 simvol ola bilər.");

        RuleFor(request => request.MemberUserIds)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage(
                "Group üzvləri göstərilməlidir.")
            .Must(memberIds =>
                memberIds.Count is >= 2 and <= 9)
            .WithMessage(
                "Group DM üçün 2–9 başqa istifadəçi seçilməlidir.")
            .Must(memberIds =>
                memberIds.All(userId => userId > 0))
            .WithMessage(
                "İstifadəçi ID-ləri düzgün olmalıdır.")
            .Must(memberIds =>
                memberIds.Distinct().Count() ==
                memberIds.Count)
            .WithMessage(
                "Eyni istifadəçi bir neçə dəfə seçilə bilməz.");
    }
}