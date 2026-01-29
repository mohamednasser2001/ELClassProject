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
        public DayOfWeek Day { get; set; } // للمتكرر
        public TimeSpan StartTime { get; set; }
        public string MeetingLink { get; set; } = string.Empty;
        public int DurationInHours { get; set; }
        public ScheduleType Type { get; set; }
        public DateTime? SpecificDate { get; set; }
        public int CourseId { get; set; }
        public Course? Course { get; set; }
        public string InstructorId { get; set; } = string.Empty;
        public Instructor? Instructor { get; set; }


        public ICollection<StudentAppointment> StudentAppointments { get; set; } = new List<StudentAppointment>();

        public bool IsActive
        {
            get
            {
                var now = DateTime.Now;
                var endTime = StartTime.Add(TimeSpan.FromHours(DurationInHours));

                if (Type == ScheduleType.OneTime && SpecificDate.HasValue)
                {
                    var startDateTime = SpecificDate.Value.Date + StartTime;
                    var endDateTime = startDateTime.AddHours(DurationInHours);
                    return now >= startDateTime && now <= endDateTime;
                }
                else
                {
                    
                    return now.DayOfWeek == Day &&
                           now.TimeOfDay >= StartTime &&
                           now.TimeOfDay <= endTime;
                }
            }

        }

    }
}
