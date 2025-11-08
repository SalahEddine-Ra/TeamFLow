

using System;
using System.ComponentModel.DataAnnotations;

namespace TeamFlowAPI.Models.DTOs
{

public class RefreshTokenRequestDto
{
    

    [Required]
    public string RequestId { get; set; } = string.Empty;  

    [Required]
    public DateTime Timestamp { get; set; }              

    public string? ClientVersion { get; set; }        
}
}