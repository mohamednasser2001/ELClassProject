namespace Models.ViewModels.Instructor
{
    public class CourseProgressVM
    {
        public string CourseName { get; set; } = string.Empty;
        public int EnrolledStudents { get; set; }
        public int SuccessRate { get; set; } 
    }
}
