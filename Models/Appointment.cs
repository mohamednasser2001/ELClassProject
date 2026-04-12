using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{

    public class Appointment
    {
        public int Id { get; set; }

      
        public DateTime StartDateTime { get; set; }
        public int DurationInHours { get; set; } = 1;

        public string MeetingLink { get; set; } = string.Empty;

        //public string? StudentId { get; set; } = string.Empty;  
        //public Student? Student { get; set; }
        public string? InstructorId { get; set; } = string.Empty;
        public Instructor? Instructor { get; set; }

        public int CourseId { get; set; }
        public Course? Course { get; set; }


        public ICollection<StudentAppointment> StudentAppointments { get; set; } = new List<StudentAppointment>();

     
        public DateTime EndDateTime => StartDateTime.AddHours(DurationInHours);

     
        public bool IsActive
        {
            get
            {
                var now = DateTime.Now;
                return now >= StartDateTime && now <= EndDateTime;
            }
        }
    }
}
