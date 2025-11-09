using TeamFlowAPI.Models.Entities;

namespace TeamFlowAPI.Services.Interfaces
{
    public interface IRefreshTokenService
    {
        // Create new refresh token for user
        Task<string> CreateRefreshTokenAsync(long userId, string ipAddress);
        
        // Validate refresh token + ROTATE it (get new one)
        Task<(bool IsValid, string? NewRefreshToken)> ValidateAndRotateTokenAsync(string token, string ipAddress);
        
        // Revoke single token (logout)
        Task<bool> RevokeTokenAsync(string token, string revokedByIp);
        
        // Revoke ALL tokens for user (password change, security breach)
        Task RevokeAllUserTokensAsync(long userId);
        
        // Clean up expired tokens
        Task CleanupExpiredTokensAsync();
    }
}