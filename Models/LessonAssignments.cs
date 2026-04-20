using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class LessonAssignments
    {
        public int Id { get; set; }
        public string FileUrl { get; set; }
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; }
    }
}
