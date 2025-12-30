using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class Course: AuditLogging
    {
        public int Id { get; set; }
        public string TitleAr { get; set; }
        public string TitleEn { get; set; }
        public string DescriptionAr { get; set; }
        public string DescriptionEn { get; set; }
        public ICollection<InstructorCourse> InstructorCourses { get; set; }
        public ICollection<StudentCourse> StudentCourses { get; set; }
    }
}
