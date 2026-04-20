using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class StudentLesson
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; }
        public string StudentId { get; set; }
        public Student Student { get; set; }
        public double Degree { get; set; }
    }
}
