using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using TeamFlowAPI.Infrastructure.Database;
using TeamFlowAPI.Services.Exceptions;
using TeamFlowAPI.Services.Interfaces;

namespace TeamFlowAPI.Services;

/// Service for validating tokens and detecting suspicious activity
public class TokenValidationService : ITokenValidationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IIpValidationService _ipValidationService;
    private readonly ILogger<TokenValidationService> _logger;

    public TokenValidationService(
        ApplicationDbContext dbContext,
        IIpValidationService ipValidationService,
        ILogger<TokenValidationService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _ipValidationService = ipValidationService ?? throw new ArgumentNullException(nameof(ipValidationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Validates token against stored hash using BCrypt
    // Optimized to filter at database level first
    public async Task<bool> ValidateTokenAsync(string token, long userId)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token validation attempted with empty token for user {UserId}", userId);
            throw new InvalidTokenException("Token cannot be empty");
        }

        // Get only non-revoked, non-expired tokens for this user from database
        var refreshTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId
                    && rt.RevokedAt == null
                    && rt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();

        if (!refreshTokens.Any())
        {
            _logger.LogWarning("No valid tokens found for user {UserId}", userId);
            return false;
        }

        // Verify token using BCrypt (done in memory after filtering)
        var isValid = refreshTokens.Any(rt => BCrypt.Net.BCrypt.Verify(token, rt.TokenHash));

        if (!isValid)
        {
            _logger.LogWarning("Token verification failed for user {UserId}", userId);
        }

        return isValid;
    }

    /// Retrieves the last known IP address for a user
    public async Task<string?> GetLastUserIpAsync(long userId)
    {
        var lastToken = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.CreatedByIp != null)
            .OrderByDescending(rt => rt.CreatedAt)
            .Select(rt => rt.CreatedByIp)
            .FirstOrDefaultAsync();

        return lastToken;
    }

    
    /// Checks for suspicious activity including IP changes and location changes
    public async Task<(bool IsSuspicious, string Reason)> CheckSuspiciousActivityAsync(long userId, string currentIp)
    {
        // Get last login record
        var lastTokenRecord = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.CreatedByIp != null)
            .OrderByDescending(rt => rt.CreatedAt)
            .FirstOrDefaultAsync();
        
        if (lastTokenRecord == null)
            return (false, "First login");  //  First time always OK
        
        // Same IP = always OK
        if (lastTokenRecord.CreatedByIp == currentIp)
            return (false, "Same IP");  // No change
        
        // Different IP - check time
        TimeSpan timeSince = DateTime.UtcNow - lastTokenRecord.CreatedAt;
        
        if (timeSince.TotalMinutes < 2) 
            return (true, "Impossible travel (IP changed in <2 min)");  // BLOCK
        
        return (false, "Legitimate IP change");  // ALLOW
    }

}
