using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class ContactUs
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Course { get; set; } = string.Empty;
        public bool IsReaded { get; set; } = false;

    }
}
