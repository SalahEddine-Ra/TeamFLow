using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
namespace TeamFlowAPI.Services.Interfaces
{
    

    public class IpValidationService : IIpValidationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<IpValidationService> _logger;
        
        // Simple in-memory cache for IP locations (can be replaced with API calls)
        private static readonly Dictionary<string, string> IpLocationCache = new();

        public IpValidationService(IConfiguration configuration, ILogger<IpValidationService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsValidIp(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                _logger.LogWarning("Empty IP address provided for validation");
                return false;
            }

            // Basic IP format validation
            if (!System.Net.IPAddress.TryParse(ipAddress, out var ip)) //tryparse: it checks if the string is a valid IP address .As argument it takes the string to be checked and an out parameter that will contain the parsed IP address if successful
            {
                _logger.LogWarning("Invalid IP format: {IpAddress}", ipAddress);
                return false;
            }

            // Check for private IP ranges if configured
            var allowPrivateIps = _configuration.GetValue<bool>("Security:AllowPrivateIPs", false);
            if (!allowPrivateIps && IsPrivateIpInternal(ip))
            {
                _logger.LogWarning("Private IP address detected: {IpAddress}", ipAddress);
                return false;
            }

            return true;
        }

        public bool IsSuspiciousActivity(string previousIp, string currentIp)
        {
            if (string.IsNullOrWhiteSpace(previousIp) || string.IsNullOrWhiteSpace(currentIp))
            {
                return false; // First login or missing data
            }

            if (previousIp == currentIp)
            {
                return false; // Same IP, not suspicious
            }

            // Get locations for both IPs
            var previousLocation = GetIpLocation(previousIp);
            var currentLocation = GetIpLocation(currentIp);

            // If locations differ, it's potentially suspicious
            bool isDifferentLocation = previousLocation != currentLocation;
            
            if (isDifferentLocation)
            {
                _logger.LogWarning("Suspicious activity: IP changed from {PreviousIp} ({PrevLocation}) to {CurrentIp} ({CurrLocation})",
                    previousIp, previousLocation, currentIp, currentLocation);
            }

            return isDifferentLocation;
        }

        public string GetIpLocation(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return "unknown";

            // Check cache first
            if (IpLocationCache.TryGetValue(ipAddress, out var cachedLocation))
                return cachedLocation;

            // Simplified geolocation - in production, use MaxMind GeoIP2 or similar service
            var location = ExtractRegionFromIp(ipAddress);
            
            // Cache the result (max 1000 entries)
            if (IpLocationCache.Count >= 1000)
                IpLocationCache.Clear();
            
            IpLocationCache[ipAddress] = location;
            return location;
        }

        public bool IsPrivateIp(string ipAddress)
        {
            if (!System.Net.IPAddress.TryParse(ipAddress, out var ip))
                return false;

            return IsPrivateIpInternal(ip);
        }

        private static string ExtractRegionFromIp(string ipAddress)
        {
            try
            {
                var parts = ipAddress.Split('.');
                if (parts.Length >= 2)
                {
                    return $"{parts[0]}.{parts[1]}";
                }
                return "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        private static bool IsPrivateIpInternal(System.Net.IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();

            // Private IP ranges:
            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // 127.0.0.0/8 (localhost)
            if (bytes[0] == 127)
                return true;

            // ::1 (IPv6 localhost)
            if (System.Net.IPAddress.IsLoopback(ip))
                return true;

            return false;
        }
    }
}
