using Discord.Core.DTOs.Conversations.Requests;
using FluentValidation;

namespace Discord.Application.Validators.Conversations;

public class UpdateGroupConversationRequestDtoValidator : AbstractValidator< UpdateGroupConversationRequestDto>
{
    public UpdateGroupConversationRequestDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage(
                "Group DM adı boş ola bilməz.")
            .MaximumLength(100)
            .WithMessage(
                "Group DM adı maksimum 100 simvol ola bilər.");
    }
}