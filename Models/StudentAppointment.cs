using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class StudentAppointment
    {
        public int Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public Student? Student { get; set; }
        public int AppointmentId { get; set; }

        public Appointment? Appointment { get; set; }
        public int TimeCount { get; set; } = 0;
    }
}
