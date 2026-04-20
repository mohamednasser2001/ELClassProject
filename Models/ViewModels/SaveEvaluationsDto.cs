using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class SaveEvaluationsDto
    {
        public int AppointmentId { get; set; }
        public int CourseId { get; set; }
        public List<StudentEvaluation> Evaluations { get; set; } = new();
    }

    public class StudentEvaluation
    {
        public string StudentId { get; set; } = string.Empty;
        public double Degree { get; set; }
    }
}
