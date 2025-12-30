using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class Instructor
    {
        public int Id { get; set; }
        public string NameAr { get; set; }
        public string NameEn { get; set; }

        public string BioAr { get; set; }
        public string BioEn { get; set; }


        public ICollection<InstructorStudent> InstructorStudents { get; set; }
        public ICollection<InstructorCourse> InstructorCourses { get; set; }
    }
}
