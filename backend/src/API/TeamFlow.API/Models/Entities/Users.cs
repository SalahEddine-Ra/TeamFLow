using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace TeamFlowAPI.Models.Entities;
public class User
{
    public long Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    public string? PasswordHash { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActve { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;



    // Navigation properties yal L mf
    [JsonIgnore]
    public List<OrganizationUser> OrganizationUsers { get; set; } = new();

    [JsonIgnore]
    public List<TeamMember> TeamMemberships { get; set; } = new();

    [JsonIgnore]
    public List<TaskAssignment> TaskAssignments { get; set; } = new();
    
    [JsonIgnore]
    public List<Comment> Comments { get; set; } = new();

    [JsonIgnore]
    public List<RefreshToken> RefreshTokens { get; set; } = new();

    [JsonIgnore]
    public PlatformAdmin? PlatformAdmin { get; set; }

}