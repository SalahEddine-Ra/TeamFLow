using System.ComponentModel.DataAnnotations;
using FluentValidation;
using FluentValidation.AspNetCore;
using TeamFlowAPI.Services.Interfaces;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;



namespace TeamFlowAPI.Models.DTOs
{
    public class RegisterDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string DisplayName { get; set; }
    }

    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        private readonly IUserService _userService;

    public RegisterDtoValidator(IUserService userService)
    {
        _userService = userService;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Valid email required")
            .MaximumLength(255).WithMessage("Email too long")
            .MustAsync(BeUniqueEmail).WithMessage("Email already registered");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain number")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain special character");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(100).WithMessage("Display name too long")
            .Matches("^[a-zA-Z0-9 ]*$").WithMessage("Display name can only contain letters, numbers and spaces");
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        return !await _userService.EmailExistsAsync(email);
    }
        
    }
}