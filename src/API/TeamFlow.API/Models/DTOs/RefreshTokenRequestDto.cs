

using System;
using System.ComponentModel.DataAnnotations;

namespace TeamFlowAPI.Models.DTOs
{

public class RefreshTokenRequestDto
{
    [Required]
    public required string RefreshToken { get; set; } 

    [Required]
    public DateTime Timestamp { get; set; }              

    public string? ClientVersion { get; set; }        
}
}