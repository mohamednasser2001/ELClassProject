using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Models
{
    [PrimaryKey("InstructorId", "CourseId")]
    public class InstructorCourse : AuditLogging
    {
        public string InstructorId { get; set; } = string.Empty;
        public Instructor Instructor { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }
    }
}
