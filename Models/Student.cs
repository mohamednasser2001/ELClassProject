using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Models
{
    public class Student
    {
        [ForeignKey("ApplicationUser")]
        public string Id { get; set; }
        [Required(ErrorMessage = "يجب عليك ادخال اسم")]
        public string NameAr { get; set; }
        [Required(ErrorMessage = "you have to enter a name")]
        public string NameEn { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        public ICollection<InstructorStudent> InstructorStudents { get; set; }
        public ICollection<StudentCourse> StudentCourses { get; set; }
    }
}
