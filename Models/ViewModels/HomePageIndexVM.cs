using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class HomePageIndexVM
    {
        public List<Models.Instructor> Instructors { get; set; } = new List<Models.Instructor>();
        public Models.HomePageContent? Content { get; set; }
        public string? SelectedCountry { get; set; }
        public List<Models.Testimonial> Testimonials { get; set; } = new();
        public List<Models.PricingPlan> PricingPlans { get; set; } = new();
        public List<Models.Article> Articles { get; set; } = new();
    }
}
