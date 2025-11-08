using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TeamFlowAPI.Models.Entities;
using TeamFlowAPI.Services.Interfaces;


namespace TeamFlowAPI.Services
{
    public class JWTService : IJwtService
    {
        private readonly IConfiguration _configuration; //IConfiguration is an interface from ASP.NET Core that lets you read settings (like the JWT secret key, issuer, and audience) from files like appsettings.json.
        public JWTService(IConfiguration configuration)
        {
                _configuration = configuration;
        }
    }
}