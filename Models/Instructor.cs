using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Models
{
    
    public class Instructor
    {
        [ForeignKey("ApplicationUser")]
        public string Id { get; set; }
        public string NameAr { get; set; }
        public string NameEn { get; set; }

        public string BioAr { get; set; }
        public string BioEn { get; set; }

        public ApplicationUser ApplicationUser { get; set; }
        public ICollection<InstructorStudent> InstructorStudents { get; set; }
        public ICollection<InstructorCourse> InstructorCourses { get; set; }
    }
}
