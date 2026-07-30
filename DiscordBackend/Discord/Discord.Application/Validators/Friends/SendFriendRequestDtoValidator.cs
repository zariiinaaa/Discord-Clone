using Discord.Core.DTOs.Friends.Requests;
using FluentValidation;

namespace Discord.Application.Validators.Friends
{
    public class SendFriendRequestDtoValidator: AbstractValidator<SendFriendRequestDto>
    {
        public SendFriendRequestDtoValidator()
        {
            RuleFor(request => request.Username)
                .NotEmpty()
                .WithMessage("İstifadəçi adı boş ola bilməz.")
                .MaximumLength(32)
                .WithMessage("İstifadəçi adı maksimum 32 simvol ola bilər.");
        }
    }
}