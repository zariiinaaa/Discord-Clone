using System;
using System.Collections.Generic;
using System.Text;

using Discord.Core.Entities.Common;

namespace Discord.Core.Entities.Privacy;

public class UserPrivacySettings : BaseEntity
{
    public int UserId { get; set; }
    public bool AllowDirectMessagesFromServerMembers
    {get;set;} = true;

    public bool EnableMessageRequests
    {get;set;} = true;

    public User User { get; set; } = null!;
}