namespace TeamFlowAPI.Services.Interfaces;

/// <summary>
/// Service for validating and managing refresh tokens with security checks
/// </summary>
public interface ITokenValidationService
{
    /// <summary>
    /// Validates token against database hash using BCrypt
    /// </summary>
    Task<bool> ValidateTokenAsync(string token, long userId);

    /// <summary>
    /// Gets the last known IP for a user
    /// </summary>
    Task<string?> GetLastUserIpAsync(long userId);

    /// <summary>
    /// Checks for suspicious activity (IP change, location change)
    /// </summary>
    Task<(bool IsSuspicious, string Reason)> CheckSuspiciousActivityAsync(long userId, string currentIp);
}
