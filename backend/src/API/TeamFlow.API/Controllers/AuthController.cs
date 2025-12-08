using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlowAPI.Models.DTOs;
using TeamFlowAPI.Services.Interfaces;
using TeamFlowAPI.Infrastructure.Database;
using TeamFlowAPI.Models.Entities;
using TeamFlowAPI.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace TeamFlowAPI.Controllers;

// ... existing using directives ...

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IRefreshTokensService _refreshTokenService;
    private readonly IUserService _userService;
    private readonly IAccessTokensService _accessTokenService;
    private readonly IIpValidationService _ipValidationService;
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IRefreshTokensService refreshTokenService,
        IUserService userService,
        IAccessTokensService accessTokenService,
        IIpValidationService ipValidationService,
        ApplicationDbContext db,
        IConfiguration config,
        ILogger<AuthController> logger)
    {
        _refreshTokenService = refreshTokenService;
        _userService = userService;
        _accessTokenService = accessTokenService;
        _ipValidationService = ipValidationService;
        _db = db;
        _config = config;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RefreshTokenResponseDto>> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _userService.RegisterUserAsync(dto);
        if (user == null) return BadRequest("Registration failed or email exists");

        var ip = GetClientIp();

        var orgUser = new OrganizationUser
        {
            UserId = user.Id,
            OrgId = _config.GetValue<long>("Auth:DefaultOrgId", 1),
            Role = "Member",
            InviteStatus = "accepted",
            IsDefault = true
        };
        _db.OrganizationUsers.Add(orgUser);
        await _db.SaveChangesAsync();

        var refresh = await _refreshTokenService.CreateRefreshTokenAsync(user.Id, ip);
        var access = _accessTokenService.GenerateAccessToken(user, orgUser, false);
        var userInfo = await BuildUserInfoAsync(user.Id);

        return Created("", new RefreshTokenResponseDto
        {
            AccessToken = access,
            RefreshToken = refresh,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:AccessMinutes", 15)),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_config.GetValue<int>("RefreshToken:ExpirationDays", 7)),
            User = userInfo
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<RefreshTokenResponseDto>> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _userService.AuthenticateAsync(dto.Email, dto.Password);
        if (user == null) return Unauthorized("Invalid credentials");

        var ip = GetClientIp();
        var refresh = await _refreshTokenService.CreateRefreshTokenAsync(user.Id, ip);

        var orgUser = await _db.OrganizationUsers
            .FirstOrDefaultAsync(ou => ou.UserId == user.Id && ou.IsDefault);

        if (orgUser == null) return BadRequest("No default organization");

        var isPlatformAdmin = await _db.PlatformAdmins.AnyAsync(pa => pa.UserId == user.Id);

        var access = _accessTokenService.GenerateAccessToken(user, orgUser, isPlatformAdmin);
        
        var userInfo = await BuildUserInfoAsync(user.Id);

        return Ok(new RefreshTokenResponseDto
        {
            AccessToken = access,
            RefreshToken = refresh,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:AccessMinutes", 15)),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_config.GetValue<int>("RefreshToken:ExpirationDays", 7)),
            User = userInfo
        });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshTokenResponseDto>> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return BadRequest("Refresh token is required");

        var ip = GetClientIp();
        var (isValid, newRefresh, userId) = await _refreshTokenService.ValidateAndRotateTokenAsync(dto.RefreshToken, ip);
        if (!isValid || newRefresh == null) return Unauthorized("Invalid or expired refresh token");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized("User not found");

        var orgUser = await _db.OrganizationUsers
            .FirstOrDefaultAsync(ou => ou.UserId == userId && ou.IsDefault);
        if (orgUser == null) return BadRequest("No default organization");

        var isPlatformAdmin = await _db.PlatformAdmins.AnyAsync(pa => pa.UserId == userId);

        var access = _accessTokenService.GenerateAccessToken(user, orgUser, isPlatformAdmin);
        var userInfo = await BuildUserInfoAsync(userId);

        return Ok(new RefreshTokenResponseDto
        {
            AccessToken = access,
            RefreshToken = newRefresh,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:AccessMinutes", 15)),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_config.GetValue<int>("RefreshToken:ExpirationDays", 7)),
            User = userInfo
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return BadRequest("Refresh token is required");

        var ip = GetClientIp();
        var revoked = await _refreshTokenService.RevokeTokenAsync(dto.RefreshToken, ip);
        return revoked ? Ok() : BadRequest("Token not found");
    }

    private string GetClientIp()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded) && _ipValidationService.IsValidIp(forwarded))
            return forwarded;

        var remote = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return _ipValidationService.IsValidIp(remote) ? remote : "unknown";
    }

    private async Task<UserInfoDto> BuildUserInfoAsync(long userId)
    {
        var ou = await _db.OrganizationUsers
            .Include(ou => ou.Organization)
            .Where(ou => ou.UserId == userId && ou.IsDefault)
            .FirstOrDefaultAsync();

        var user = await _db.Users.FirstAsync(u => u.Id == userId);

        var isPlatformAdmin = await _db.PlatformAdmins.AnyAsync(pa => pa.UserId == userId);
        return new UserInfoDto
        {
            Id = user.Id,
            DisplayName = user.DisplayName ?? "",
            Role = ou?.Role ?? "Member",
            CurrentOrgId = ou?.OrgId ?? 0,
            CurrentOrgName = ou?.Organization?.Name ?? "",
            IsPlatformAdmin = isPlatformAdmin
        };
    }
}