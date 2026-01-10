using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Models
{
    public class Lesson
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter lesson title")]
     
        public string Title { get; set; }

        public DateTime LectureDate { get; set; }= DateTime.Now;
        public string DriveLink { get; set; }
        public string LecturePdfUrl { get; set; } 
        public string AssignmentPdfUrl { get; set; } 

      
        public int CourseId { get; set; }
        public Course Course { get; set; }
    }
}
