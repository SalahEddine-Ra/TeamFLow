using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamFlowAPI.Models.DTOs;
using TeamFlowAPI.Models.Entities;
using TeamFlowAPI.Services;
using TeamFlowAPI.Services.Exceptions;
using TeamFlowAPI.Services.Interfaces;
using TeamFlowAPI.Infrastructure.Database;


namespace TeamFlowAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IAccessTokenService _accessTokenService;
        private readonly IUserService _userService;
        private readonly PasswordService _passwordService;
        private readonly IIpValidationService _ipValidationService;
        private readonly ITokenValidationService _tokenValidationService;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IRefreshTokenService refreshTokenService,
                        IAccessTokenService accessTokenService,
                        IUserService userService,
                        PasswordService passwordService,
                        IIpValidationService ipValidationService,
                        ITokenValidationService tokenValidationService,
                        ApplicationDbContext applicationDbContext,
                        ILogger<AuthController> logger)
        {
            _refreshTokenService = refreshTokenService ?? throw new ArgumentException(nameof(refreshTokenService));
            _accessTokenService = accessTokenService ?? throw new ArgumentException(nameof(accessTokenService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
            _ipValidationService = ipValidationService ?? throw new ArgumentNullException(nameof(ipValidationService));
            _tokenValidationService = tokenValidationService ?? throw new ArgumentNullException(nameof(tokenValidationService));
            _dbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        // Endpoint 1: registre

        [HttpPost("register")]
        public async Task<ActionResult<object>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                _logger.LogInformation($"Register attempt for email: {registerDto.Email}");


                // Register user using _userService.RegisterUserAsync()
                var registeredUser = await _userService.RegisterUserAsync(registerDto);
                if (!registeredUser)
                {
                    return BadRequest(new { error = "Registration failed" });
                }

                // Return 201 Created with user email and display name
                return CreatedAtAction(nameof(Register), new { email = registerDto.Email }, new
                {
                    message = "User registered successfully",
                    email = registerDto.Email,
                    displayName = registerDto.DisplayName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Register error: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Endpoint 2: login
        [HttpPost("login")]
        public async Task<ActionResult<Object>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                _logger.LogInformation($"Login attempt for email: {loginDto.Email}");
                var isUserExists = await _userService.ValidateCredentialsAsync(loginDto.Email, loginDto.Password);
                if (!isUserExists)
                {
                    return Unauthorized(new { error = "Invalid email or password" });
                }
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

                var userOrganization = await _dbContext.OrganizationUsers
                    .Include(ou => ou.Organization)
                    .FirstOrDefaultAsync(ou => ou.UserId == user.Id);

                var clientIp = GetClientIp();
                var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.Id, clientIp);
                var accessToken = _accessTokenService.GenerateAccessToken(user, userOrganization);

                return Ok(new
                {
                    accessToken = accessToken,
                    refreshToken = refreshToken
                });

            }catch (Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}