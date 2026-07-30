using System;
using System.Collections.Generic;
using System.Text;
using Discord.Core.DTOs.Conversations.Requests;
using FluentValidation;

namespace Discord.Application.Validators.Conversations;

public class CreateDirectConversationRequestDtoValidator: AbstractValidator<CreateDirectConversationRequestDto>
{
    public CreateDirectConversationRequestDtoValidator()
    {
        RuleFor(request => request.OtherUserId)
            .GreaterThan(0)
            .WithMessage(
                "İstifadəçi ID-si düzgün deyil.");
    }
}
