using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Models
{
    [PrimaryKey("InstructorId", "StudentId")]
    public class InstructorStudent : AuditLogging
    {
        public string InstructorId { get; set; }
        public Instructor Instructor { get; set; }

        public string StudentId { get; set; }
        public Student Student { get; set; }
    }
}
