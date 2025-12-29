using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    internal class InstructorStudent
    {
        public int InstructorId { get; set; }
        public Instructor Instructor { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; }
    }
}
