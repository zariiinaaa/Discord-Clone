using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Exceptions
{
    public class UnauthorizedException: Exception
    {
        public UnauthorizedException(string message)
        : base(message) { }
    }
}
