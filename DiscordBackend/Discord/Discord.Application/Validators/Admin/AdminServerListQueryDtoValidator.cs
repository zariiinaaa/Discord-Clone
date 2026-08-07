using Discord.Core.DTOs.Admin.Requests;
using FluentValidation;

namespace Discord.Application.Validators.Admin;

public class AdminServerListQueryDtoValidator : AbstractValidator<AdminServerListQueryDto>
{
    public AdminServerListQueryDtoValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Search).MaximumLength(256)
            .When(query => !string.IsNullOrWhiteSpace(query.Search));
    }
}
