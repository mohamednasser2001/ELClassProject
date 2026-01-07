using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class StudentCoursesVM
    {
        public int CourseId { get; set; }
        public string CourseTitleAr { get; set; }
        public string CourseTitleEn { get; set; }
        // ممكن تضيف الصورة لو حابب تعرضها
        public string? CourseImage { get; set; }
    }
}
