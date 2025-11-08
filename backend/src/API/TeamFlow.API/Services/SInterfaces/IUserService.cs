
using System.Threading.Tasks;
using TeamFlowAPI.Models.DTOs;

namespace TeamFlowAPI.Services.Interfaces
{
    public interface IUserService
    {
        // Email validation
        Task<bool> EmailExistsAsync(string Email);

        // User registration
        Task<bool> RegisterUserAsync(RegisterDto registerDto);

        Task<bool> ValidateCredentialsAsync(string Email, string password);
    }
}