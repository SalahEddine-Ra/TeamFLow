using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using TeamFlowAPI.Models.Entities;
namespace TeamFlowAPI.Services;

public class PasswordService
{
    private readonly PasswordHasher<User> _passwordHasher;

    public PasswordService()
    {
        _passwordHasher = new PasswordHasher<User>();
    }

    public string HashPassword(User user, string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
            throw new ArgumentException("Password cannot be null or empty");

        return _passwordHasher.HashPassword(user, plainPassword);
    }

    public bool VerifyPassword(User user, string hashedPassword, string plainPassword)
    {

        if (string.IsNullOrEmpty(hashedPassword))
            throw new ArgumentException("Password cannot be null or empty");
        if (string.IsNullOrEmpty(plainPassword))
            throw new ArgumentException("Password cannot be null or empty");
        var result = _passwordHasher.VerifyHashedPassword(user, hashedPassword, plainPassword);

        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;

    
    }
}

