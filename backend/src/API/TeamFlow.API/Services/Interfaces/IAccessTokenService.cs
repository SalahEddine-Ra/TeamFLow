using System.Security.Claims;
using TeamFlowAPI.Models.Entities;

namespace TeamFlowAPI.Services.Interfaces;

public interface IAccessTokenService
{
    string GenerateAccessToken(User user, OrganizationUser organizationUser);
    ClaimsPrincipal? ValidateToken(string token);
    bool IsTokenExpired(string token);
}