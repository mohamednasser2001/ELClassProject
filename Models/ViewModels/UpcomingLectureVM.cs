using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class UpcomingLectureVM
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string SubjectName { get; set; }
        public string TeacherName { get; set; }
        public string ZoomLink { get; set; }
    }
}
