namespace Service.Security;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiresMinutes { get; set; } = 60;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
            throw new InvalidOperationException("Jwt: Key is required.");
        
        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("Jwt: Issuer is required.");
        
        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Jwt: Audience is required.");
    }
    
}