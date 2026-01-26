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
        public string Id { get; set; } = string.Empty;
        [Required(ErrorMessage = "يجب عليك ادخال اسم")]
        public string NameAr { get; set; } = string.Empty;
        [Required(ErrorMessage = "you have to enter a name")]
        public string NameEn { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public ApplicationUser ApplicationUser { get; set; } = null!;
        public ICollection<InstructorStudent> InstructorStudents { get; set; } = new List<InstructorStudent>();
        public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
        public ICollection<StudentAppointment> StudentAppointments { get; set; } = new List<StudentAppointment>();
    }
}
