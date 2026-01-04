using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels.Student
{
    public class StudentDetailsVM
    {
        public Models.Student Student { get; set; } =null!;
        public List<InstructorStudent> InstructorStudents { get; set; } = new List<InstructorStudent>();
        public List<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
    }
}
