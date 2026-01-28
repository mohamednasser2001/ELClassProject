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

        public string? CourseImage { get; set; }

        public int AttendedCount { get; set; }
        public int GoalCount { get; set; } = 16;

        public int ProgressPercent
        {
            get
            {
                if (GoalCount <= 0) return 0;
                var p = (int)Math.Round((AttendedCount * 100.0) / GoalCount);
                return Math.Min(100, Math.Max(0, p));
            }
        }
    }
}
