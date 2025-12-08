using TeamFlowAPI.Models.DTOs;
using TeamFlowAPI.Services.Interfaces;
using TeamFlowAPI.Infrastructure.Database;
using TeamFlowAPI.Models.Entities;
using System.Security.Cryptography;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using TeamFlowAPI.Services.Exceptions;

namespace TeamFlowAPI.Services
{
    /// Service for managing refresh tokens with security validations
    public class RefreshTokenService : IRefreshTokensService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly ITokenValidationService _tokenValidationService;
        private readonly IIpValidationService _ipValidationService;
        private readonly ILogger<RefreshTokenService> _logger;

        public RefreshTokenService(
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            ITokenValidationService tokenValidationService,
            IIpValidationService ipValidationService,
            ILogger<RefreshTokenService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _tokenValidationService = tokenValidationService ?? throw new ArgumentNullException(nameof(tokenValidationService));
            _ipValidationService = ipValidationService ?? throw new ArgumentNullException(nameof(ipValidationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// Creates a new refresh token for a user
        public async Task<string> CreateRefreshTokenAsync(long userId, string ipAddress)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ipAddress))
                {
                    throw new ArgumentException("IP address cannot be empty", nameof(ipAddress));
                }

                // Validate user exists and is active
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null || !user.IsActive)
                {
                    throw new TokenServiceException($"User {userId} not found or is inactive");
                }

                // Validate IP format
                if (!_ipValidationService.IsValidIp(ipAddress))
                {
                    throw new TokenServiceException($"Invalid IP address: {ipAddress}");
                }

                // Generate random token
                var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

                // Hash token using BCrypt
                var workFactor = _configuration.GetValue<int>("RefreshToken:BcryptWorkFactor", 12);
                var tokenHash = BCrypt.Net.BCrypt.HashPassword(token, workFactor);

                // Create token entity
                var refreshToken = new RefreshToken
                {
                    TokenHash = tokenHash,
                    UserId = userId,
                    ExpiresAt = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("RefreshToken:ExpirationDays", 7)),
                    CreatedByIp = ipAddress
                };

                await _dbContext.RefreshTokens.AddAsync(refreshToken);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Refresh token created for user {UserId} from IP {IpAddress}", userId, ipAddress);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating refresh token for user {UserId}", userId);
                throw;
            }
        }

        /// Validates and rotates a refresh token
public async Task<(bool IsValid, string? NewRefreshToken, long UserId)> ValidateAndRotateTokenAsync(string token, string ipAddress)
{
    try
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidTokenException("Token cannot be empty");
        }
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("IP address cannot be empty", nameof(ipAddress));
        }
        // Validate IP format
        if (!_ipValidationService.IsValidIp(ipAddress))
        {
            _logger.LogWarning("Invalid IP format in token rotation: {IpAddress}", ipAddress);
            return (false, null, 0);
        }
        // Filter database query at database level 
        var refreshTokens = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .Where(rt => rt.ExpiresAt > DateTime.UtcNow
                    && rt.RevokedAt == null
                    && rt.User.IsActive)
            .OrderByDescending(rt => rt.CreatedAt)
            .Take(10)
            .ToListAsync();
        // Verify token using BCrypt (in-memory after filtering)
        var refreshToken = refreshTokens.FirstOrDefault(rt => BCrypt.Net.BCrypt.Verify(token, rt.TokenHash));
        if (refreshToken == null)
        {
            _logger.LogWarning("Token verification failed - no matching token found");
            return (false, null, 0);
        }
        // Validate IP matches the token's creation IP
        if (refreshToken.CreatedByIp != ipAddress)
        {
            _logger.LogWarning("Token IP mismatch for user {UserId}",
                refreshToken.UserId);
            return (false, null, 0);
        }
        // Check for suspicious activity
        var (isSuspicious, reason) = await _tokenValidationService.CheckSuspiciousActivityAsync(refreshToken.UserId, ipAddress);
        if (isSuspicious)
        {
            _logger.LogWarning("Suspicious activity detected for user {UserId}: {Reason}", refreshToken.UserId, reason);
            throw new SuspiciousActivityException(reason);
        }
        // Revoke old token
        refreshToken.RevokedAt = DateTime.UtcNow;
        // Create new token
        var newToken = await CreateRefreshTokenAsync(refreshToken.UserId, ipAddress);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Token rotated successfully for user {UserId}", refreshToken.UserId);
        return (true, newToken, refreshToken.UserId);
    }
    catch (SuspiciousActivityException ex)
    {
        _logger.LogWarning(ex, "Suspicious activity detected during token rotation");
        return (false, null, 0);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error validating and rotating token");
        throw;
    }
}
        /// Revokes a single refresh token
        public async Task<bool> RevokeTokenAsync(string token, string revokedByIp)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new ArgumentException("Token cannot be empty", nameof(token));
                }

                if (string.IsNullOrWhiteSpace(revokedByIp))
                {
                    throw new ArgumentException("IP address cannot be empty", nameof(revokedByIp));
                }

                // Get non-revoked tokens for active users only
                var refreshTokens = await _dbContext.RefreshTokens
                    .Include(rt => rt.User)
                    .Where(rt => rt.RevokedAt == null && rt.User.IsActive)
                    .ToListAsync();

                // Verify token using BCrypt (in-memory after filtering)
                var refreshToken = refreshTokens.FirstOrDefault(rt => BCrypt.Net.BCrypt.Verify(token, rt.TokenHash));

                if (refreshToken == null)
                {
                    _logger.LogWarning("Token not found for revocation");
                    return false;
                }

                // Revoke the token
                refreshToken.RevokedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Token revoked for user {UserId}", refreshToken.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                throw;
            }
        }

        /// Revokes all tokens for a user (password change,etc)
        public async Task RevokeAllUserTokensAsync(long userId)
        {
            try
            {
                var userTokens = await _dbContext.RefreshTokens
                    .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                    .ToListAsync();

                if (userTokens.Count == 0)
                {
                    _logger.LogInformation("No active tokens found for user {UserId}", userId);
                    return;
                }

                foreach (var token in userTokens)
                {
                    token.RevokedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Revoked {TokenCount} tokens for user {UserId}", userTokens.Count, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all tokens for user {UserId}", userId);
                throw;
            }
        }

        /// Cleans up expired tokens from the database
        public async Task CleanupExpiredTokensAsync()
        {
            try
            {
                var expiredTokens = await _dbContext.RefreshTokens
                    .Where(rt => rt.ExpiresAt <= DateTime.UtcNow)
                    .ToListAsync();

                if (expiredTokens.Count == 0)
                {
                    _logger.LogInformation("No expired tokens to clean up");
                    return;
                }

                _dbContext.RefreshTokens.RemoveRange(expiredTokens);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {ExpiredTokenCount} expired tokens", expiredTokens.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired tokens");
                throw;
            }
        }
    }
}

