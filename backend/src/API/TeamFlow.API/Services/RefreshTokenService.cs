using TeamFlowAPI.Models.DTOs;
using TeamFlowAPI.Services.Interfaces;
using TeamFlowAPI.Infrastructure.Database;
using TeamFlowAPI.Models.Entities;
using System.Security.Cryptography;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
namespace TeamFlowAPI.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _DbContext;

        // constructor to inject IConfiguration and ApplicationDbContext
        public RefreshTokenService(IConfiguration configuration, ApplicationDbContext DbContext)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _DbContext = DbContext ?? throw new ArgumentNullException(nameof(DbContext));
        }

        // Method to generate a refresh token
        public async Task<string> CreateRefreshTokenAsync(long userId, string ipAddress)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var HashToken = BCrypt.Net.BCrypt.HashPassword(token, _configuration.GetValue<int>("RefreshToken:BcryptWorkFactor", 12));

            await _DbContext.RefreshTokens.AddAsync(new RefreshToken
            {
                TokenHash = HashToken,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("RefreshToken:ExpirationDays", 7)),
                CreatedByIp = ipAddress
            });
            await _DbContext.SaveChangesAsync();
            return token;
        }

        // Method to validate and rotate a refresh token
        public async Task<(bool IsValid, string? NewRefreshToken)> ValidateAndRotateTokenAsync(string token, string ipAddress)
        {

            var hashedToken = BCrypt.Net.BCrypt.HashPassword(token, _configuration.GetValue<int>("RefreshToken:BcryptWorkFactor", 12));
            var existingToken = await _DbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hashedToken && rt.ExpiresAt > DateTime.UtcNow && rt.RevokedAt == null && rt.User.IsActve);

            bool ValidIpAdress = existingToken != null && existingToken.CreatedByIp == ipAddress;
            
            if (existingToken != null && ValidIpAdress)
            {
                existingToken.RevokedAt = DateTime.UtcNow;

                var newToken = await CreateRefreshTokenAsync(existingToken.UserId, ipAddress);
                await _DbContext.SaveChangesAsync();
                return (true, newToken);
            }
            else
            {
                return (false, null);
            }
        }
    }
}
