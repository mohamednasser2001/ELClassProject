using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Models.ViewModels
{
    public class RegisterVM
    {
        // أضفنا \s للسماح بالمسافات بين الأسماء
        [RegularExpression(@"^[A-Za-z\s]*$", ErrorMessage = "English characters only are allowed")]
        [Display(Name = "Name (English)")]
        public string? NameEN { get; set; }

        // الـ Regex الخاص بك للعربية صحيح جداً
        [RegularExpression(@"^[\u0600-\u06FF\s]+$", ErrorMessage = "يسمح بالحروف العربية فقط")]
        [Display(Name = "الاسم (بالعربي)")]
        public string? NameAR { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(32, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 32 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}
