using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class LastLessonVM
    {
        public int LessonId { get; set; }
        public int CourseId { get; set; }
        public string LessonTitle { get; set; } = "";
        public string CourseTitle { get; set; } = "";
        public DateTime? LectureDate { get; set; }
        public string? DriveLink { get; set; }
        public string? LecturePdfUrl { get; set; }
        public string? AssignmentPdfUrl { get; set; }

    }
}
