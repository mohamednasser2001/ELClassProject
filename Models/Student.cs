using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    internal class Student
    {
        public int Id { get; set; }
        public string NameAr { get; set; }
        public string NameEn { get; set; }

        public ICollection<InstructorStudent> InstructorStudents { get; set; }
        public ICollection<StudentCourse> StudentCourses { get; set; }
    }
}
