using System;

namespace Models
{
    public class InstructorStudentMonthPayment
    {
        public int Id { get; set; }
        public string InstructorId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
