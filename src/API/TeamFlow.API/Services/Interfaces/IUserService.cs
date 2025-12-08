using System.Threading.Tasks;
using TeamFlowAPI.Models.DTOs;
using TeamFlowAPI.Models.Entities;

namespace TeamFlowAPI.Services.Interfaces;

public interface IUserService
{
    Task<bool> EmailExistsAsync(string email);
    Task<User?> RegisterUserAsync(RegisterDto registerDto);
    Task<User?> AuthenticateAsync(string email, string password);  
    
}