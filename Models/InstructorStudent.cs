using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Models
{
    [PrimaryKey("InstructorId", "StudentId")]
    public class InstructorStudent : AuditLogging
    {
        public string InstructorId { get; set; } = string.Empty;
        public Instructor Instructor { get; set; } = null!;

        public string StudentId { get; set; } = string.Empty;
        public Student Student { get; set; } = null!;
    }
}
