using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class LastLessonVM
    {
        public int LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;

        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;

        public DateTime? AttendedAt { get; set; }
    }
}
