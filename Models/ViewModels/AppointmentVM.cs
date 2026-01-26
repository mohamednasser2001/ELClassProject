using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class AppointmentVM
    {
        public string InstructorId { get; set; }
        public string MeetingLink { get; set; }
        public int TimeCount { get; set; } = 1;
        public int Type { get; set; } // 0 for Recurring, 1 for OneTime
        public int DurationInHours { get; set; }
        public DayOfWeek Day { get; set; }
        public DateTime? SpecificDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public int CourseId { get; set; }
        public string StudentId { get; set; }
    }
}
