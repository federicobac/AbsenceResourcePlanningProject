using Facet;
using Infra;

namespace Service.RequestDtos;

[Facet(sourceType: typeof(User), exclude:
[
    nameof(User.UserId),
    nameof(User.PasswordHash),
    nameof(User.Role)
])]
public partial class UpdateUserRequestDto
{
    public string? Password { get; set; }
}