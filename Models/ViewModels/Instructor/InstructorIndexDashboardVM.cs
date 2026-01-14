using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels.Instructor
{
    public class InstructorIndexDashboardVM
    {
        public int TotalStudents { get; set; }
        public int ActiveLessons { get; set; }
        public int TotalCourses { get; set; }
        public int TotalInstructors { get; set; } // إذا كان المقصود زملاؤه في المنصة

        // لإظهار نسب النمو أو بيانات إضافية
        public int StudentsThisMonth { get; set; }
        public List<CourseProgressVM> TopPerformingCourses { get; set; }
    }
}
