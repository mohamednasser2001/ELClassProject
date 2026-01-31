using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Models
{
    public class StudentAppointment
    {
        public int Id { get; set; }

        public string StudentId { get; set; } = string.Empty;
        public Student? Student { get; set; }
        [ForeignKey("AppointmentId")]
        public int AppointmentId { get; set; }
        public Appointment? Appointment { get; set; }

        
        public DateTime? StudentExpiryDate { get; set; }

        
        public int TimeCount { get; set; } = 0;

        public bool IsAttended { get; set; }
        public DateTime? AttendedAt { get; set; }
        public bool IsAccessAllowed
        {
            get
            {
                
                var now = DateTime.Now;
                return Appointment != null &&
                       Appointment.IsActive &&
                       (!StudentExpiryDate.HasValue || now.Date <= StudentExpiryDate.Value.Date);
            }
        }
    }
}
