using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels.Student
{
    public class AppointmentNotifyVM
    {
        public int AppointmentId { get; set; }
        public string CourseName { get; set; } = "";
        public string InstructorName { get; set; } = "";
        public string MeetingLink { get; set; } = "";

        // وقت المحاضرة الفعلي (هنحسبه من recurring/oneTime)
        public DateTime StartsAt { get; set; }

        // نص جاهز للعرض (هنملاه في ViewComponent)
        public string DisplayText { get; set; } = "";
    }
}
