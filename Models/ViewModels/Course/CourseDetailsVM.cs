using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels.Course
{
    public class CourseDetailsVM
    {
        public Models.Course Course { get; set; } = null!;
        public List<InstructorCourse> InstructorCourses { get; set; } = new List<InstructorCourse>();
        public List<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
    }
}
