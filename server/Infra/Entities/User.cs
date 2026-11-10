using LinqToDB.Mapping;

namespace Infra;

public class User
{
    [PrimaryKey]
    public string UserId  { get; set; }
    public string UserName  { get; set; }
    public string PasswordHash  { get; set; }
    public string Role { get; set; }
}