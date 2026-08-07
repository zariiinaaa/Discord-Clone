using Discord.Core.DTOs.ChannelPermissions.Requests;
using Discord.Core.Enums;
using FluentValidation;

namespace Discord.Application.Validators.ChannelPermissions;

public class UpdateChannelPermissionOverridesRequestDtoValidator: AbstractValidator<UpdateChannelPermissionOverridesRequestDto>
{
    private static readonly HashSet<ServerPermission>
        AllowedChannelPermissions = new()
        {
            ServerPermission.ViewChannels,
            ServerPermission.ManageChannels,
            ServerPermission.CreateInvites,
            ServerPermission.SendMessages,
            ServerPermission.ManageMessages,
            ServerPermission.ReadMessageHistory,
            ServerPermission.AddReactions,
            ServerPermission.EmbedLinks,
            ServerPermission.AttachFiles,
            ServerPermission.MentionEveryone,
            ServerPermission.Connect,
            ServerPermission.Speak,
            ServerPermission.MuteMembers,
            ServerPermission.DeafenMembers,
            ServerPermission.MoveMembers
        };

    public UpdateChannelPermissionOverridesRequestDtoValidator()
    {
        RuleFor(request => request.Overrides)
            .NotNull()
            .WithMessage(
                "Permission override siyahısı boş obyekt ola bilməz.");

        RuleFor(request => request.Overrides)
            .Must(HaveUniquePermissions)
            .WithMessage(
                "Eyni permission bir request daxilində təkrar göndərilə bilməz.");

        RuleForEach(request => request.Overrides)
            .ChildRules(overrideItem =>
            {
                overrideItem
                    .RuleFor(item => item.Permission)
                    .Must(permission =>
                        Enum.IsDefined(permission))
                    .WithMessage(
                        "Permission dəyəri düzgün deyil.");

                overrideItem
                    .RuleFor(item => item.Permission)
                    .Must(permission =>
                        AllowedChannelPermissions.Contains(
                            permission))
                    .WithMessage(
                        "Bu permission kanal səviyyəsində dəyişdirilə bilməz.");

                overrideItem
                    .RuleFor(item => item.OverrideType)
                    .Must(overrideType =>
                        Enum.IsDefined(overrideType))
                    .WithMessage(
                        "Override növü düzgün deyil.");
            });
    }

    private static bool HaveUniquePermissions(
        List<ChannelPermissionOverrideItemRequestDto> overrides)
    {
        if (overrides is null)
        {
            return true;
        }

        return overrides
            .Select(overrideItem =>
                overrideItem.Permission)
            .Distinct()
            .Count() == overrides.Count;
    }
}