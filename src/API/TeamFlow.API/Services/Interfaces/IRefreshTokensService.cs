using System.Threading.Tasks;
using TeamFlowAPI.Services.Exceptions;

namespace TeamFlowAPI.Services.Interfaces;

public interface IRefreshTokensService
{
    Task<string> CreateRefreshTokenAsync(long userId, string ipAddress);
    Task<(bool IsValid, string? NewRefreshToken, long UserId)> ValidateAndRotateTokenAsync(string token, string ipAddress);
    Task<bool> RevokeTokenAsync(string token, string revokedByIp);
    Task RevokeAllUserTokensAsync(long userId);
    Task CleanupExpiredTokensAsync();
}