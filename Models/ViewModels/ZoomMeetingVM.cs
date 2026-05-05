using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class ZoomMeetingVM
    {
        public string MeetingNumber { get; set; } = "";
        public string Signature { get; set; } = "";
        public string UserName { get; set; } = "";
        public string ZoomSdkKey { get; set; } = "";
        public string Password { get; set; } = "";
        public int StudentAppointmentId { get; set; }
    }


    public class MarkAttendedRequest
    {
        public int StudentAppointmentId { get; set; }
    }

}
