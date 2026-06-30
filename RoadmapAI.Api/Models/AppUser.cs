using Microsoft.AspNetCore.Identity;

namespace RoadmapAI.Api.Models;

// Extends Identity's built-in user with our custom fields
public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
