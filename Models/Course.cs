using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Models
{
    public class Course: AuditLogging
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "من فضلك أدخل عنوان")]
        [MinLength(3 , ErrorMessage = " العنوان يجب ان يكون اكثر من 3 حروف")]
        public string TitleAr { get; set; }
        [Required(ErrorMessage ="Please, Enter a title")]
        [MinLength(3, ErrorMessage = " title must be more than 3 letters")]
        public string TitleEn { get; set; }
        [Required(ErrorMessage =" من فضلك ادخل وصف")]
        public string? DescriptionAr { get; set; }
        [Required(ErrorMessage ="please, Enter a Description")]
        public string? DescriptionEn { get; set; }
        public ICollection<InstructorCourse> InstructorCourses { get; set; }
        public ICollection<StudentCourse> StudentCourses { get; set; }
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    }
}
