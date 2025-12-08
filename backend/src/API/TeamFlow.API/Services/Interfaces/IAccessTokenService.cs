using System.Security.Claims;
using TeamFlowAPI.Models.Entities;

namespace TeamFlowAPI.Services.Interfaces;

public interface IAccessTokensService
{
    string GenerateAccessToken(User user, OrganizationUser organizationUser, bool isPlatformAdmin);
    ClaimsPrincipal? ValidateToken(string token);
    bool IsTokenExpired(string token);

}