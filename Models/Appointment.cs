using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{


    public enum ScheduleType
    {
        Recurring, 
        OneTime    
    }
    public class Appointment
    {
        public int Id { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeSpan StartTime { get; set; }
        public string MeetingLink { get; set; } = string.Empty;
        public int DurationInHours { get; set; }
        public ScheduleType Type { get; set; }
        public DateTime? SpecificDate { get; set; }
        public int CourseId { get; set; }
        public Course? Course { get; set; }

    }
}
