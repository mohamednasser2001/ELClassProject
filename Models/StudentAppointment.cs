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

        public bool IsAttended { get; set; }
        public DateTime? AttendedAt { get; set; }
        
    }
}
