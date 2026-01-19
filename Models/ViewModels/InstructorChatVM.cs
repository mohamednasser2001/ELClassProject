using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class InstructorChatVM
    {
        public string InstructorId { get; set; }
        public string InstructorNameEn { get; set; }
        public string InstructorNameAr { get; set; }
        public string InstructorImage { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }
    }
}
