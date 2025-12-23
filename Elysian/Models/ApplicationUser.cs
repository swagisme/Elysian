using Microsoft.AspNetCore.Identity;

namespace Elysian.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? ProfilePicture { get; set; }
    }
}