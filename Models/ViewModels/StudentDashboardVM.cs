using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class StudentDashboardVM
    {
        public IEnumerable<StudentCoursesVM> Courses { get; set; }
        public IEnumerable<InstructorChatVM> Instructors { get; set; }

        public int? NextStudentAppointmentId { get; set; }
        public bool CanJoinNow { get; set; }

        public int TotalLessons { get; set; }      
        public int CompletedLessons { get; set; }   
        public int OverallProgress { get; set; }    
        public int NewNotifications { get; set; }

        public int TotalCoursesCount { get; set; }
    }
}
