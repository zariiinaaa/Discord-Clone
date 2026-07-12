using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Exceptions
{
    public class ConflictException:Exception
    {
        public ConflictException(string message)
        : base(message) { }
    }
}
