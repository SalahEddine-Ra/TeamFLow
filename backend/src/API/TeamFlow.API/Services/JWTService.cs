using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TeamFlowAPI.Models.Entities;
using TeamFlowAPI.Services.Interfaces;

namespace TeamFlowAPI.Services
{
    public class JWTService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JWTService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string GenerateAccessToken(User user, OrganizationUser organizationUser)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            if (organizationUser == null)
                throw new ArgumentNullException(nameof(organizationUser));

            // Claims
            var claims = new[]
            {
                new Claim("userId", user.Id.ToString()),
                new Claim("displayName", user.DisplayName ?? string.Empty),
                new Claim("role", organizationUser.Role),              
                new Claim("orgId", organizationUser.OrgId.ToString()), 
                new Claim("orgName", organizationUser.Organization.Name) 
            };

            // Load JWT configuration
            var secretKey = _configuration["Jwt:SecretKey"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("JWT SecretKey is missing in configuration.");

            // Security key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["Jwt:SecretKey"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            
            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("JWT SecretKey is missing in configuration.");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No grace period for expiration
            };
            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return principal;
            }
            catch (SecurityTokenException)
            {
                return null;
            }
        }

        public bool IsTokenExpired(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
            var JwtToken = tokenHandler.ReadToken(token);
            return JwtToken.ValidTo < DateTime.UtcNow;
            }
            catch
            {

                return true;
            }
            
        }
    }
}
