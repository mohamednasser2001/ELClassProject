using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Models
{
    [PrimaryKey("StudentId", "CourseId")]
    public class StudentCourse : AuditLogging
    {
        public string StudentId { get; set; } = string.Empty;
        public Student Student { get; set; } = null!;

        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;


    }
}
