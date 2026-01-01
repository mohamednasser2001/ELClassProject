using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Models
{
    
    public class Instructor
    {
        [ForeignKey("ApplicationUser")]
        public string Id { get; set; } = string.Empty;
        [Required(ErrorMessage ="يجب عليك ادخال اسم")]
        public string NameAr { get; set; } = string.Empty;
         [Required(ErrorMessage = "you have to enter a name")]
        public string NameEn { get; set; } = string.Empty;
        [Required(ErrorMessage = " السيرة الذاتية مطلوبة")]

        public string BioAr { get; set; } = string.Empty;
        [Required(ErrorMessage = "the bio is required")]
        public string BioEn { get; set; } = string.Empty;

        public ApplicationUser ApplicationUser { get; set; } = null!;
        public ICollection<InstructorStudent> InstructorStudents { get; set; } = new List<InstructorStudent>();
        public ICollection<InstructorCourse> InstructorCourses { get; set; } = new List<InstructorCourse>();
    }
}
