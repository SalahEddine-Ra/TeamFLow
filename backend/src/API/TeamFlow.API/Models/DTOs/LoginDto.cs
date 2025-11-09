using System.ComponentModel.DataAnnotations;
using FluentValidation;
using FluentValidation.AspNetCore;
using TeamFlowAPI.Services;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;



namespace TeamFlowAPI.Models.DTOs
{
    public class LoginDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Valid email format required");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
}