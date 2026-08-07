using Discord.Core.Enums;
using Discord.Core.Models.Auth;

namespace Discord.Core.Interfaces;

public interface IOneTimeTokenService
{
    GeneratedOneTimeToken Generate(AuthTokenPurpose purpose);
    string HashToken(string token);
}
