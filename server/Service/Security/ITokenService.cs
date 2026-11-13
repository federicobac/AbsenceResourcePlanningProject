using Infra;

namespace Service.Security;

public class ITokenService
{
    string CreateToken(User user);
}