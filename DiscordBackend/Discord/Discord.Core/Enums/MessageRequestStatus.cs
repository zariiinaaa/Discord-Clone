using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Enums;

public enum MessageRequestStatus
{
    Pending = 0,
    Accepted = 1,
    Ignored = 2,
    Spam = 3
}
