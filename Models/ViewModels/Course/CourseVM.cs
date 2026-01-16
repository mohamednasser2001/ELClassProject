using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels.Course
{
    public class CourseVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
