using System;
using System.Collections.Generic;
using System.Text;
using Models;

namespace Models.ViewModels.Instructor
{
    public class InstructorIndexDashboardVM
    {
        public int TotalStudents { get; set; }
        public int ActiveLessons { get; set; }
        public int TotalCourses { get; set; }
        public int TotalInstructors { get; set; } 
        public int StudentsThisMonth { get; set; }
        public List<Models.Student> Students { get; set; }
        public List<CourseProgressVM> TopPerformingCourses { get; set; }
     
    }
}
