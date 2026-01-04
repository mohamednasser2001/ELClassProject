using System;
using System.Collections.Generic;
using System.Text;
using Models;
namespace Models.ViewModels.Instructor
{
    public class InstructorDetailsVM
    {
        public Models.Instructor Instructor { get; set; }
        public List<InstructorCourse> InstructorCourses { get; set; } = new List<InstructorCourse>();
        public List<InstructorStudent> InstructorStudents { get; set; } = new List<InstructorStudent>();
    }
}
