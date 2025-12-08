using Geolocation ;
using Microsoft.EntityFrameworkCore;
using TeamFlowAPI.Infrastructure.Database;
using TeamFlowAPI.Services.Interfaces;
using TeamFlowAPI.Services.Exceptions;
using TeamFlowAPI.Services;

namespace TeamFlowAPI.Services;

public class TokenValidationService : ITokenValidationService
{
    private readonly ApplicationDbContext _db;
    private readonly IIpValidationService _ipValidationService;
    private readonly IConfiguration _config;
    private readonly ILogger<TokenValidationService> _logger;

    public TokenValidationService(
        ApplicationDbContext db,
        IIpValidationService ipValidationService,
        IConfiguration config,
        ILogger<TokenValidationService> logger)
    {
        _db = db;
        _ipValidationService = ipValidationService;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> ValidateTokenAsync(string token, long userId)
    {
        var hashes = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .Select(rt => rt.TokenHash)
            .ToListAsync();

        return hashes.Any(hash => BCrypt.Net.BCrypt.Verify(token, hash));
    }

    public async Task<string?> GetLastUserIpAsync(long userId)
    {
        return await _db.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .OrderByDescending(rt => rt.CreatedAt)
            .Select(rt => rt.CreatedByIp)
            .FirstOrDefaultAsync();
    }

    public async Task<(bool IsSuspicious, string Reason)> CheckSuspiciousActivityAsync(long userId, string currentIp)
    {
        var lastIp = await GetLastUserIpAsync(userId);
        if (string.IsNullOrWhiteSpace(lastIp)) return (false, "");

        var curLoc = _ipValidationService.GetIpLocation(currentIp);
        var lastLoc = _ipValidationService.GetIpLocation(lastIp);
        if (curLoc.City == null || lastLoc.City == null) return (false, "");

        var distanceKm = GeoCalculator.GetDistance(
            new Coordinate(curLoc.Latitude, curLoc.Longitude),
            new Coordinate(lastLoc.Latitude, lastLoc.Longitude),
            1,
            DistanceUnit.Kilometers);

        var maxDist = _config.GetValue<double>("Geo:MaxDistanceKm", 1000);
        if (distanceKm > maxDist)
            return (true, $"Location jump: {distanceKm:F0} km");

        var lastTime = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .OrderByDescending(rt => rt.CreatedAt)
            .Select(rt => rt.CreatedAt)
            .FirstOrDefaultAsync();

        var hours = (DateTime.UtcNow - lastTime).TotalHours;
        var maxSpeed = _config.GetValue<double>("Geo:MaxSpeedKph", 800);
        if (hours > 0 && distanceKm / hours > maxSpeed)
            return (true, "Impossible travel speed");

        return (false, "");
    }
}