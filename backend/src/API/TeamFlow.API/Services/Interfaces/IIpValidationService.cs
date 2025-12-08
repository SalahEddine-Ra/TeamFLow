namespace TeamFlowAPI.Services.Interfaces
{
    public interface IIpValidationService
    {
        bool IsValidIp(string ipAddress);
        bool IsSuspiciousActivity(string previousIp, string currentIp);
        (double Latitude, double Longitude, string City) GetIpLocation(string ipAddress);
        bool IsPrivateIp(string ipAddress);
    }
}