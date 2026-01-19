using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class HomePageIndexVM
    {
        public List<Models.Instructor> Instructors { get; set; } = new List<Models.Instructor>();
    }
}
