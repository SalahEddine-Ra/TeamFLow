using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamFlowAPI.Services.Interfaces;

namespace TeamFlowAPI.Services;

public class IpValidationService : IIpValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<IpValidationService> _logger;
    private readonly HttpClient _http;   // injected via IHttpClientFactory

    // Thread-safe cache: IP → (lat, lon, city)
    private static readonly ConcurrentDictionary<string,
        (double Latitude, double Longitude, string City)> _cache = new();

    public IpValidationService(
        IConfiguration configuration,
        ILogger<IpValidationService> logger,
        IHttpClientFactory httpFactory)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger        = logger        ?? throw new ArgumentNullException(nameof(logger));
        _http          = httpFactory.CreateClient(nameof(IpValidationService));
        _http.Timeout = TimeSpan.FromSeconds(5);
    }

    #region --- IP validation & private-IP checks ---------------------------------

    public bool IsValidIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            _logger.LogWarning("Empty IP address provided for validation");
            return false;
        }

        if (!System.Net.IPAddress.TryParse(ipAddress, out var ip))
        {
            _logger.LogWarning("Invalid IP format: {IpAddress}", ipAddress);
            return false;
        }

        var allowPrivate = _configuration.GetValue<bool>("Security:AllowPrivateIPs", false);
        if (!allowPrivate && IsPrivateIpInternal(ip))
        {
            _logger.LogWarning("Private IP address detected: {IpAddress}", ipAddress);
            return false;
        }

        return true;
    }

    public bool IsPrivateIp(string ipAddress) =>
        System.Net.IPAddress.TryParse(ipAddress, out var ip) && IsPrivateIpInternal(ip);

    private static bool IsPrivateIpInternal(System.Net.IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();

        // IPv4 private ranges
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            if (bytes[0] == 10) return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            if (bytes[0] == 127) return true;
        }

        // IPv6 link-local / site-local / unique-local / loopback
        if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal)
            return true;

        return System.Net.IPAddress.IsLoopback(ip);
    }

    #endregion

    #region --- Geolocation (free ip-api.com) ------------------------------------

    public (double Latitude, double Longitude, string City) GetIpLocation(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return (0, 0, "unknown");

        if (_cache.TryGetValue(ipAddress, out var cached))
            return cached;

        try
        {
            var url = $"http://ip-api.com/json/{ipAddress}?fields=status,message,lat,lon,city";
            var resp = _http.GetAsync(url).GetAwaiter().GetResult();
            resp.EnsureSuccessStatusCode();

            var json = resp.Content.ReadFromJsonAsync<Dictionary<string, object>>()
                           .GetAwaiter().GetResult();

            if (json != null && json.TryGetValue("status", out var s) && s?.ToString() == "fail")
            {
                var msg = json.TryGetValue("message", out var m) ? m?.ToString() : "unknown";
                _logger.LogWarning("IP lookup failed for {Ip}: {Msg}", ipAddress, msg);
                return (0, 0, "unknown");
            }

            var lat  = json != null && json.TryGetValue("lat",  out var l) ? Convert.ToDouble(l) : 0;
            var lon  = json != null && json.TryGetValue("lon",  out var o) ? Convert.ToDouble(o) : 0;
            var city = json != null && json.TryGetValue("city", out var c) ? c?.ToString() ?? "unknown" : "unknown";

            var result = (lat, lon, city);

            // keep cache small
            if (_cache.Count >= 1000) _cache.Clear();
            _cache[ipAddress] = result;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get location for IP: {Ip}", ipAddress);
            return (0, 0, "unknown");
        }
    }

    #endregion

    #region --- Suspicious activity (distance) ----------------------------------

    public bool IsSuspiciousActivity(string previousIp, string currentIp)
    {
        if (string.IsNullOrWhiteSpace(previousIp) || string.IsNullOrWhiteSpace(currentIp))
            return false;

        if (previousIp == currentIp)
            return false;

        var (prevLat, prevLon, _) = GetIpLocation(previousIp);
        var (curLat , curLon , _) = GetIpLocation(currentIp);

        if (prevLat == 0 && prevLon == 0 || curLat == 0 && curLon == 0)
            return false; // no location data

        var distanceKm = CalculateDistance(prevLat, prevLon, curLat, curLon);
        var maxDist    = _configuration.GetValue<double>("Geo:MaxDistanceKm", 1000);

        if (distanceKm > maxDist)
        {
            _logger.LogWarning(
                "Suspicious activity: IP changed from {Prev} to {Curr} ({Dist:F0} km)",
                previousIp, currentIp, distanceKm);
            return true;
        }

        return false;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth radius km
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        lat1 = ToRadians(lat1);
        lat2 = ToRadians(lat2);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double deg) => deg * (Math.PI / 180);

    #endregion
}