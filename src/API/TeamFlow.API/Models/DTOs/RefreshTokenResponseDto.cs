using System;
using System.ComponentModel.DataAnnotations;

namespace TeamFlowAPI.Models.DTOs
{
    public class UserInfoDto
    {
        public long Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;      
        public long CurrentOrgId { get; set; }                  
        public string CurrentOrgName { get; set; } = string.Empty;
        public bool IsPlatformAdmin { get; set; }
    }


    public class RefreshTokenResponseDto
    {
        [Required]
        public required string AccessToken { get; set; }

        [Required]
        public required string RefreshToken { get; set; }

        [Required]
        public required DateTime ExpiresAt { get; set; }

        [Required]
        public required DateTime RefreshTokenExpiresAt { get; set; }

        [Required]
        public required UserInfoDto User { get; set; }
    }
}