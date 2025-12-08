using Microsoft.EntityFrameworkCore;
using TeamFlowAPI.Infrastructure.Database;
using TeamFlowAPI.Models.DTOs;
using TeamFlowAPI.Models.Entities;
using TeamFlowAPI.Services.Interfaces;

namespace TeamFlowAPI.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UserService> _logger;
    private readonly PasswordService _passwordService;

    public UserService(ApplicationDbContext db, ILogger<UserService> logger, PasswordService passwordService)
    {
        _db = db;
        _logger = logger;
        _passwordService = passwordService;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant());
    }

    public async Task<User?> RegisterUserAsync(RegisterDto dto)
    {
        try
        {
            if (await EmailExistsAsync(dto.Email))
                return null;  // Or throw InvalidOperationException("Email exists")

            var user = new User
            {
                Email = dto.Email.ToLowerInvariant(),
                DisplayName = dto.DisplayName,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            user.PasswordHash = _passwordService.HashPassword(user, dto.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _logger.LogInformation("User registered: {Email}", dto.Email);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for {Email}", dto.Email);
            return null;
        }
    }

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant() && u.IsActive);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash)) return null;

        if (!_passwordService.VerifyPassword(user, user.PasswordHash, password))
            return null;

        _logger.LogInformation("User authenticated: {Email}", email);
        return user;
    }
}