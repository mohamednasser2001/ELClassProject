using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Models.ViewModels.Student
{
    public class StudentProfileVM
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [ValidateNever]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [ValidateNever]
        public string? ProfileImageUrl { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePicture { get; set; }

        // Password fields
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        [ValidateNever]
        public string Initials =>
            string.IsNullOrWhiteSpace(FullName)
                ? "??"
                : string.Concat(
                    FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x[0])
                  ).ToUpper();
    }
}
