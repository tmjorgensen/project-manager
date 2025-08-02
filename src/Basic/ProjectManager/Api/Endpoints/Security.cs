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

        if (string.IsNullOrWhiteSpace(body.UserName))
            return Results.BadRequest("Username cnnot be empty.");

        try
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Auth:Secret"]!)
            );

            var claims = new List<Claim>
        {
            new("sub", body.UserName)
        };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = configuration["Auth:Issuer"],
                Audience = configuration["Auth:Audience"],
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    securityKey,
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
            logger.LogError(ex, "Sign in failed.");

            throw;
        }
    }
}
