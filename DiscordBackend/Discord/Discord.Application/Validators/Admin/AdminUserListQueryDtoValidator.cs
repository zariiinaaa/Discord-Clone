using Discord.Core.DTOs.Admin.Requests;
using FluentValidation;

namespace Discord.Application.Validators.Admin;

public class AdminUserListQueryDtoValidator : AbstractValidator<AdminUserListQueryDto>
{
    public AdminUserListQueryDtoValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Search).MaximumLength(256)
            .When(query => !string.IsNullOrWhiteSpace(query.Search));
        RuleFor(query => query.Role)
            .Must(role => !role.HasValue || Enum.IsDefined(role.Value))
            .WithMessage("Platform rolu etibarsızdır.");
    }
}
