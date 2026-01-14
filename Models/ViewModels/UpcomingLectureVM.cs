using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class UpcomingLectureVM
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string SubjectName { get; set; }
<<<<<<< HEAD
        public string InstructorName { get; set; }
=======
        public string TeacherName { get; set; }
>>>>>>> 21059d53a3fcba0dcba9805a914dd4af4ec8f05b
        public string ZoomLink { get; set; }
    }
}
