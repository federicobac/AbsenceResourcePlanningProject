using System.Security.AccessControl;
using Facet;
using Infra;

namespace Service;

[Facet(sourceType:typeof(User),exclude:[nameof(User.PasswordHash)]
    )]
    public partial class UserDto;