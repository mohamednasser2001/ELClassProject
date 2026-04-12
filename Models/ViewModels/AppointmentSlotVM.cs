using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class AppointmentSlotVM
    {
        public DateTime StartDateTime { get; set; }
        public int DurationInHours { get; set; }
        public string MeetingLink { get; set; } = string.Empty;
    }

    public class BulkAppointmentVM
    {
        public List<string> StudentIds { get; set; } = new(); 
        public int CourseId { get; set; }
        public List<AppointmentSlotVM> Appointments { get; set; } = new();
    }
}
