using Discord.Core.Enums;
using System;
using System.Collections.Generic;
namespace Discord.Core.Enums;

public enum ServerPermission
{
    ViewChannels = 1,
    ManageChannels = 2,
    ManageServer = 3,
    ManageRoles = 4,

    CreateInvites = 5,
    KickMembers = 6,
    BanMembers = 7,
    ModerateMembers = 8,

    ChangeNickname = 9,
    ManageNicknames = 10,

    SendMessages = 11,
    ManageMessages = 12,
    ReadMessageHistory = 13,
    AddReactions = 14,

    EmbedLinks = 15,
    AttachFiles = 16,
    MentionEveryone = 17,

    Connect = 18,
    Speak = 19,
    MuteMembers = 20,
    DeafenMembers = 21,
    MoveMembers = 22,

    ManageExpressions = 23,
    ViewAuditLog = 24,
    Administrator = 25
}
