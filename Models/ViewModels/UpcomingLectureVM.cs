using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class UpcomingLectureVM
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public string ZoomLink { get; set; } = string.Empty;
    }
}
