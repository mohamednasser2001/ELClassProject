using Microsoft.AspNetCore.Identity;

namespace Models
{
    public class ApplicationUser:IdentityUser
    {
        public string Name { get; set; }
        public string? Address { get; set; }
        public string? Img { get; set; }
        public Student Student { get; set; }
        public Instructor Instructor { get; set; }

    }
}
