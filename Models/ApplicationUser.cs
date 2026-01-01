using Microsoft.AspNetCore.Identity;

namespace Models
{
    public class ApplicationUser:IdentityUser
    {
        public string NameAR { get; set; }
        public string NameEN { get; set; }

        public string? AddressAR { get; set; }
        public string? AddressEN { get; set; }

        public string? Img { get; set; }
        public Student Student { get; set; }
        public Instructor Instructor { get; set; }

    }
}
