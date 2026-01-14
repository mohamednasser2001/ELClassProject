using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class StudentDashboardVM
    {
        public IEnumerable<StudentCoursesVM> Courses { get; set; }

        // لستة المدرسين بتوع الشات
        public IEnumerable<InstructorChatVM> Instructors { get; set; }
    }
}
