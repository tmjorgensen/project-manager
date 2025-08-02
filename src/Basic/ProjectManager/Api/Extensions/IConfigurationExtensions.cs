namespace Api.Extensions;

public static class IConfigurationExtensions
{
    public static void GetAuthSettings(this IConfiguration configuration, out string secret, out string issuer, out string audience)
    {
        secret = configuration["Auth:Secret"]
            ?? throw new Exception("Configuration missing for Auth:Secret.");
        issuer = configuration["Auth:Issuer"]
            ?? throw new Exception("Configuration missing for Auth:Issuer.");
        audience = configuration["Auth:Audience"]
            ?? throw new Exception("Configuration missing for Auth:Audience.");

    }
}
