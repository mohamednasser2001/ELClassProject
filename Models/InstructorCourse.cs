using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class InstructorCourse
    {
        public int InstructorId { get; set; }
        public Instructor Instructor { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }
    }
}
