using Api.Extensions;
using Api.Models.Requests;
using Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Api.Endpoints;

public static class Security
{
    public static IResult SignIn([FromBody]SigninDto body, IConfiguration configuration, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(nameof(SignIn));

        try
        { 
            if (string.IsNullOrWhiteSpace(body.UserName))
                throw new ArgumentException("Username cnnot be empty.");

            configuration.GetAuthSettings(out string secret, out string issuer, out string audience);

            var claims = new List<Claim>
            {
                new("sub", body.UserName)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = issuer,
                Audience = audience,
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);

            return Results.Ok(new SecurityTokensDto(accessToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sign-in failed.");

            return ex.ToErrorResult("Sign-in failed.");
        }
    }
}
