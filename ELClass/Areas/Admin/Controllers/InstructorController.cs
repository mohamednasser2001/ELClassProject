using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using Models.ViewModels.Instructor;
using System.Globalization;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ELClass.Areas.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    [Area("Admin")]
    public class InstructorController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly UserManager<ApplicationUser> userManager;

        public InstructorController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            this.unitOfWork = unitOfWork;
            this.userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }



        public async Task<IActionResult> Details(string id)
        {
            var instructor = await unitOfWork.InstructorRepository.GetOneAsync(e => e.Id == id, include: e => e.Include(e => e.ApplicationUser));
            if (instructor == null)
            {
                return View("AdminNotFoundPage");
            }

            // 1. جلب الكورسات الخاصة بالمحاضر
            var courses = await unitOfWork.InstructorCourseRepository.GetAsync(
                filter: e => e.InstructorId == id,
                include: e => e.Include(e => e.Course)
            );
            var courseList = courses.ToList();

            
            var directStudents = await unitOfWork.InstructorStudentRepository.GetAsync(filter: e => e.InstructorId == id);
            var directStudentIds = directStudents.Select(s => s.StudentId).ToList();

            
            var instructorCourseIds = courseList.Select(c => c.CourseId).ToList();

            
            var courseStudents = await unitOfWork.StudentCourseRepository.GetAsync(
                filter: sc => instructorCourseIds.Contains(sc.CourseId)
            );
            var courseStudentIds = courseStudents.Select(sc => sc.StudentId).ToList();

            
            ViewBag.TotalStudentsCount = directStudentIds.Union(courseStudentIds).Distinct().Count();

            var model = new InstructorDetailsVM()
            {
                Instructor = instructor,
                InstructorCourses = courseList,
                //InstructorStudents = directStudents.ToList() 
            };

            return View(model);
        }



        public async Task<IActionResult> AssignCourse(int courseId, string instructorId)
        {
            if (courseId == 0 || string.IsNullOrEmpty(instructorId))
                return BadRequest();

            var instructorCourse = new InstructorCourse
            {
                CourseId = courseId,
                InstructorId = instructorId,
                CreatedAt = DateTime.Now,
                CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            await unitOfWork.InstructorCourseRepository.CreateAsync(instructorCourse);
            await unitOfWork.CommitAsync();

            return Json(new { success = true });
        }

        public async Task<IActionResult> AssignStudent(string studentId, string instructorId)
        {
            if (studentId != null && instructorId != null)
            {
                var instructorStudent = new InstructorStudent
                {
                    StudentId = studentId,
                    InstructorId = instructorId,
                    CreatedAt = DateTime.Now,
                    CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier)
                };
                await unitOfWork.InstructorStudentRepository.CreateAsync(instructorStudent);
                await unitOfWork.CommitAsync();
            

                return Json(new { success = true });

            }
            else
            {
                return View("AdminNotFoundPage");
            }
        }
        


        public async Task<IActionResult> RemoveCourse(int courseId, string instructorId)
        {
            var course = await unitOfWork.InstructorCourseRepository.GetOneAsync(filter: e => e.InstructorId == instructorId && e.CourseId == courseId);
            if (course == null)
            {
                return View("AdminNotFoundPage");
            }

            var result = await unitOfWork.InstructorCourseRepository.DeleteAsync(course);
            if (result)
            {
                var suc = await unitOfWork.CommitAsync();
                if (suc)
                {

                    return RedirectToAction("details", new { id = instructorId });
                }

            }
            return BadRequest();
        }

        public async Task<IActionResult> RemoveStudent(string studentId, string instructorId)
        {
            var student = await unitOfWork.InstructorStudentRepository.GetOneAsync(filter: e => e.InstructorId == instructorId && e.StudentId == studentId);
            if (student == null)
            {
                return View("AdminNotFoundPage");
            }

            var result = await unitOfWork.InstructorStudentRepository.DeleteAsync(student);
            if (result)
            {
                var suc = await unitOfWork.CommitAsync();
                if (suc)
                {

                    return RedirectToAction("details", new { id = instructorId });
                }

            }
            return BadRequest();
        }



        public async Task<IActionResult> SearchStudents(string term, string instructorId)
        {
            var students = await unitOfWork.StudentRepository.GetAsync(filter: e =>
            (e.NameAr.Contains(term) || e.NameEn.Contains(term)));


            var insStdunt = await unitOfWork.InstructorStudentRepository.GetAsync(filter: e => e.InstructorId == instructorId);
            if (insStdunt.Any())
            {
                var assignedStudentIds = insStdunt.Select(ic => ic.StudentId).ToList();
                students = students.Where(c => !assignedStudentIds.Contains(c.Id)).ToList();
            }

            var result = students
                .Select(c => new
                {
                    id = c.Id,
                    text = $"{c.NameEn} - {c.NameAr}"
                });

            return Json(result);
        }
        public async Task<IActionResult> SearchCourses(string term, string instructorId)
        {
            var courses = await unitOfWork.CourseRepository.GetAsync(filter: e =>
            (e.TitleAr.Contains(term) || e.TitleEn.Contains(term)));


            var stdCourses = await unitOfWork.InstructorCourseRepository.GetAsync(filter: e => e.InstructorId == instructorId);
            if (stdCourses.Any())
            {
                var assignedCourseIds = stdCourses.Select(ic => ic.CourseId).ToList();
                courses = courses.Where(c => !assignedCourseIds.Contains(c.Id)).ToList();
            }

            var result = courses
                .Select(c => new
                {
                    id = c.Id,
                    text = $"{c.TitleEn} - {c.TitleAr}"
                });

            return Json(result);
        }

        public async Task<IActionResult> SearchUsers(string term)
        {
            var users = await userManager.Users
                .Where(u =>
                    u.UserName!.Contains(term) ||
                    u.Email!.Contains(term) || u.NameEN!.Contains(term) || u.NameAR!.Contains(term))
                .Select(u => new
                {
                    id = u.Id,
                    text = u.UserName + " (" + u.Email + ")"
                })

                .ToListAsync();

            var instructorsId = (await unitOfWork.InstructorRepository.GetAsync())
                .Select(e => e.Id);

            if (instructorsId.Any())
            {
                users = users.Where(e => !instructorsId.Contains(e.id)).ToList();
            }

            return Json(users);
        }

        [HttpPost]
        public async Task<IActionResult> GetInstructors()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = int.Parse(Request.Form["start"].FirstOrDefault() ?? "0");
                var length = int.Parse(Request.Form["length"].FirstOrDefault() ?? "10");
                var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";
                var lang = Request.Form["language"].FirstOrDefault() ??
                   (CultureInfo.CurrentCulture.Name.StartsWith("ar") ? "ar" : "en");

                Expression<Func<Models.Instructor, bool>> filter = c => string.IsNullOrEmpty(searchValue) ||
                    (c.NameEn.Contains(searchValue) || c.NameAr.Contains(searchValue) ||
                     c.BioEn.Contains(searchValue) || c.BioAr.Contains(searchValue));

                var instructors = await unitOfWork.InstructorRepository.GetAsync(
                    filter: filter,
                    skip: start,
                    take: length,
                    orderBy: q => q.OrderBy(i => i.Id)
                );

                var totalRecords = await unitOfWork.InstructorRepository.CountAsync();
                var filteredRecords = await unitOfWork.InstructorRepository.CountAsync(filter: filter);

                var result = instructors.Select(c => new
                {
                    id = c.Id,
                    name = lang == "en" ? c.NameEn : c.NameAr,
                    bio = lang == "en" ? c.BioEn : c.BioAr
                }).ToList();

                return Json(new { draw, recordsTotal = totalRecords, recordsFiltered = filteredRecords, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { draw = Request.Form["draw"].FirstOrDefault(), recordsTotal = 0, recordsFiltered = 0, data = new List<object>(), error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetInstructorCourses()
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = int.Parse(Request.Form["start"].FirstOrDefault() ?? "0");
            var length = int.Parse(Request.Form["length"].FirstOrDefault() ?? "10");
            var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";
            var instructorId = Request.Form["instructorId"].FirstOrDefault();

            Expression<Func<InstructorCourse, bool>> filter = e => e.InstructorId == instructorId &&
                (string.IsNullOrEmpty(searchValue) || e.Course.TitleEn.Contains(searchValue) || e.Course.TitleAr.Contains(searchValue));

            var courses = await unitOfWork.InstructorCourseRepository.GetAsync(
                filter: filter,
                include: e => e.Include(x => x.Course),
                orderBy: q => q.OrderByDescending(x => x.CreatedAt ?? DateTime.MinValue),
                skip: start,
                take: length
            );

            var totalCount = await unitOfWork.InstructorCourseRepository.CountAsync(filter: e => e.InstructorId == instructorId);
            var filteredCount = await unitOfWork.InstructorCourseRepository.CountAsync(filter: filter);

            var result = courses.Select(c => new
            {
                courseId = c.CourseId,
                title = c.Course.TitleEn
            }).ToList();

            return Json(new { draw, recordsTotal = totalCount, recordsFiltered = filteredCount, data = result });
        }

        [HttpPost]
        public async Task<IActionResult> GetInstructorStudents()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = int.Parse(Request.Form["start"].FirstOrDefault() ?? "0");
                var length = int.Parse(Request.Form["length"].FirstOrDefault() ?? "10");
                var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";
                var instructorId = Request.Form["instructorId"].FirstOrDefault();

                
                var directStudents = await unitOfWork.InstructorStudentRepository.GetAsync(
                    filter: e => e.InstructorId == instructorId
                );
                var directIds = directStudents.Select(s => s.StudentId);

                
                var courseStudents = await unitOfWork.StudentCourseRepository.GetAsync(
                    filter: e => e.Course.InstructorCourses.Any(ic => ic.InstructorId == instructorId)
                );
                var courseIds = courseStudents.Select(s => s.StudentId);

                
                var allUniqueStudentIds = directIds.Union(courseIds).ToList();

                
                Expression<Func<Student, bool>> studentFilter = s =>
                    allUniqueStudentIds.Contains(s.Id) &&
                    (string.IsNullOrEmpty(searchValue) || s.NameEn.Contains(searchValue) || s.NameAr.Contains(searchValue));

                
                var totalCount = allUniqueStudentIds.Count;
                var filteredCount = await unitOfWork.StudentRepository.CountAsync(filter: studentFilter);

                
                var students = await unitOfWork.StudentRepository.GetAsync(
                    filter: studentFilter,
                    orderBy: q => q.OrderBy(s => s.NameEn),
                    skip: start,
                    take: length
                );

                var result = students.Select(s => new
                {
                    studentId = s.Id,
                    name = s.NameEn
                }).ToList();

                return Json(new { draw, recordsTotal = totalCount, recordsFiltered = filteredCount, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { draw = Request.Form["draw"].FirstOrDefault(), recordsTotal = 0, recordsFiltered = 0, data = new List<object>(), error = ex.Message });
            }
        }

        public IActionResult CreateInstructorAccount()
        {
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInstructorAccount(
    Models.Instructor ins,
    string Password,
    string ConfirmPassword)
        {
            
            bool isArabic = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft;

            ModelState.Remove("ApplicationUser.Student");
            ModelState.Remove("ApplicationUser.Instructor");
            ModelState.Remove("ApplicationUser.NameAR");
            ModelState.Remove("ApplicationUser.NameEn");

            if (!ModelState.IsValid)
            {
                string errorMsg = isArabic
                    ? "عفواً، هناك خطأ في البيانات المدخلة"
                    : "Sorry, there is an error in the input data";
                ModelState.AddModelError("", errorMsg);
                return View(ins);
            }

            if (Password != ConfirmPassword)
            {
                string passMsg = isArabic
                    ? "كلمة المرور غير متطابقة"
                    : "Passwords do not match";
                ModelState.AddModelError("", passMsg);
                return View(ins);
            }

            var email = ins.ApplicationUser.Email;
            var userName = email!.Split('@')[0] + new Random().Next(10, 99);

            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                PhoneNumber = ins.ApplicationUser.PhoneNumber,
                AddressEN = ins.ApplicationUser.AddressEN,
                AddressAR = ins.ApplicationUser.AddressAR,
                NameAR = ins.NameAr,
                NameEN = ins.NameEn,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                TempData["Error"] = isArabic
                    ? "فشل إنشاء حساب المستخدم"
                    : "Failed to create user account";
                return View(ins);
            }

            await userManager.AddToRoleAsync(user, "Instructor");

            ins.Id = user.Id;
            ins.CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ins.CreatedAt = DateTime.Now;
            ins.ApplicationUser = user;

            await unitOfWork.InstructorRepository.CreateAsync(ins);
            await unitOfWork.CommitAsync();

            TempData["Success"] = isArabic
                ? "تم إنشاء حساب المحاضر بنجاح"
                : "Instructor account has been created successfully";

            return RedirectToAction("Index");
        }

        public IActionResult Create()
        {

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Models.Instructor ins)
        {
            var lang = HttpContext.Session.GetString("Language") ?? "en";

            ModelState.Remove("ApplicationUser");
            ModelState.Remove("InstructorStudents");
            ModelState.Remove("InstructorCourses");

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", lang == "en" ? "Please correct the errors in the form" : "يرجى تصحيح الأخطاء في النموذج");
                return View(ins);
            }

            var user = await userManager.FindByIdAsync(ins.Id);
            if (user == null)
            {
                ModelState.AddModelError("", lang == "en" ? "User not found" : "المستخدم غير موجود");
                return View(ins);
            }

            var oldStudentCourses = await unitOfWork.StudentCourseRepository.GetAsync(sc => sc.StudentId == ins.Id);
            foreach (var sc in oldStudentCourses)
            {
                await unitOfWork.StudentCourseRepository.DeleteAsync(sc);
            }

            var oldInstructorStudents = await unitOfWork.InstructorStudentRepository.GetAsync(isd => isd.StudentId == ins.Id);
            foreach (var isd in oldInstructorStudents)
            {
                await unitOfWork.InstructorStudentRepository.DeleteAsync(isd);
            }

            var std = await unitOfWork.StudentRepository.GetOneAsync(s => s.Id == ins.Id);
            if (std != null)
            {
                await unitOfWork.StudentRepository.DeleteAsync(std);
            }

            var newInstructor = new Models.Instructor
            {
                Id = ins.Id,
                NameAr = ins.NameAr,
                NameEn = ins.NameEn,
                BioAr = ins.BioAr,
                BioEn = ins.BioEn,
                SpecializationEn = ins.SpecializationEn,
                SpecializationAr = ins.SpecializationAr,  
                CreatedById= User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedAt = DateTime.Now
            };

            await unitOfWork.InstructorRepository.CreateAsync(newInstructor);

            var roles = await userManager.GetRolesAsync(user);
            if (roles.Contains("Student"))
            {
                await userManager.RemoveFromRoleAsync(user, "Student");
            }

            if (!roles.Contains("Instructor"))
            {
                await userManager.AddToRoleAsync(user, "Instructor");
            }

            var commit = await unitOfWork.CommitAsync();

            if (!commit)
            {
                ModelState.AddModelError("", lang == "en" ? "Something went wrong during save" : "حدث خطأ أثناء الحفظ");
                return View(ins);
            }

            TempData["Success"] = lang == "en" ? "Instructor added successfully" : "تمت إضافة المدرب بنجاح";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInstructorDetails(InstructorDetailsVM model)
        {
            var lang = HttpContext.Session.GetString("Language") ?? "en";
            var ins = model.Instructor;

            ModelState.Remove("Instructor.ApplicationUser");
            ModelState.Remove("InstructorCourses");
            ModelState.Remove("InstructorStudents");
            ModelState.Remove("Instructor.ApplicationUser.Student");
            ModelState.Remove("Instructor.ApplicationUser.Instructor");

            if (ModelState.IsValid)
            {
                try
                {
                    var instructorInDb = await unitOfWork.InstructorRepository.GetOneAsync(
                        e => e.Id == ins.Id,
                        include: query => query.Include(e => e.ApplicationUser)
                    );

                    if (instructorInDb == null) return NotFound();

                    instructorInDb.NameEn = ins.NameEn;
                    instructorInDb.NameAr = ins.NameAr;
                    instructorInDb.BioEn = ins.BioEn;
                    instructorInDb.BioAr = ins.BioAr;
                    instructorInDb.SpecializationEn = ins.SpecializationEn;
                    instructorInDb.SpecializationAr = ins.SpecializationAr;

                    if (instructorInDb.ApplicationUser != null && ins.ApplicationUser != null)
                    {
                        var user = instructorInDb.ApplicationUser;
                        user.Email = ins.ApplicationUser.Email;
                        user.PhoneNumber = ins.ApplicationUser.PhoneNumber;
                        user.AddressEN = ins.ApplicationUser.AddressEN;
                        user.AddressAR = ins.ApplicationUser.AddressAR;

                        await userManager.UpdateNormalizedEmailAsync(user);
                    }

                    await unitOfWork.CommitAsync();

                    TempData["Success"] = lang == "en" ? "Instructor updated successfully" : "تم تحديث بيانات المحاضر بنجاح";
                    return RedirectToAction("Details", new { id = ins.Id });
                }
                catch (Exception ex)
                {
                    TempData["error"] = (lang == "en" ? "Error while saving: " : "حدث خطأ أثناء الحفظ: ") + ex.Message;
                }
            }

            var courses = await unitOfWork.InstructorCourseRepository.GetAsync(e => e.InstructorId == ins.Id, include: i => i.Include(c => c.Course));
            var students = await unitOfWork.InstructorStudentRepository.GetAsync(e => e.InstructorId == ins.Id, include: i => i.Include(s => s.Student));

            model.InstructorCourses = courses.ToList();
            model.InstructorStudents = students.ToList();

            if (TempData["error"] is null)
            {
                TempData["error"] = lang == "en" ? "Please check the input data." : "يرجى التحقق من البيانات المدخلة.";
            }

            return View("Details", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeProfilePhoto(string instructorId, IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                TempData["Error"] = "Please select a valid image.";
                return RedirectToAction("Details", new { id = instructorId });
            }

            try
            {

                var instructor = await unitOfWork.InstructorRepository.GetOneAsync(
                    u => u.Id == instructorId,
                    include: e => e.Include(e => e.ApplicationUser));

                if (instructor == null || instructor.ApplicationUser == null)
                {
                    TempData["Error"] = "Instructor not found.";
                    return RedirectToAction("Index");
                }


                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "users");


                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath = Path.Combine(folderPath, fileName);


                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }


                if (!string.IsNullOrEmpty(instructor.ApplicationUser.Img))
                {
                    var oldPath = Path.Combine(folderPath, instructor.ApplicationUser.Img);
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }


                instructor.ApplicationUser.Img = fileName;

                await unitOfWork.CommitAsync();

                TempData["Success"] = "Profile photo updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred: " + ex.Message;
            }

            return RedirectToAction("Details", new { id = instructorId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var instructor = await unitOfWork.InstructorRepository.GetOneAsync(
                c => c.Id == id,
                include: i => i.Include(u => u.ApplicationUser));

            if (instructor == null)
            {
                return Json(new { success = false, message = "Instructor not found" });
            }
            var userCourses = await unitOfWork.CourseRepository.GetAsync(c => c.CreatedById == instructor.Id);
            
            var user = instructor.ApplicationUser;
            using var transaction = await unitOfWork.BeginTransactionAsync();
            try
            {

                var relatedInstructors = await unitOfWork.InstructorRepository.GetAsync(isd => isd.CreatedById == id);
                foreach (var ins in relatedInstructors)
                {
                    ins.CreatedById = null;
                    await unitOfWork.InstructorRepository.EditAsync(ins);
                }

                foreach (var course in userCourses)
                {
                    course.CreatedById = null;
                    await unitOfWork.CourseRepository.EditAsync(course);
                }
                var instructorCourses = await unitOfWork.InstructorCourseRepository.GetAsync(ic => ic.InstructorId == id);
                foreach (var ic in instructorCourses)
                {
                    await unitOfWork.InstructorCourseRepository.DeleteAsync(ic);
                }

                var instructorStudents = await unitOfWork.InstructorStudentRepository.GetAsync(isd => isd.InstructorId == id);
                foreach (var isd in instructorStudents)
                {
                    await unitOfWork.InstructorStudentRepository.DeleteAsync(isd);
                }

                await unitOfWork.InstructorRepository.DeleteAsync(instructor);
                await unitOfWork.CommitAsync();

                if (user != null)
                {
                    DeleteUserImage(user.Img);
                    var result = await userManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        return Json(new { success = false, message = result.Errors.FirstOrDefault()?.Description });
                    }
                }
                await transaction.CommitAsync();
                return Json(new { success = true });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return Json(new
                {
                    success = false,
                    message = "Cannot delete instructor. They are set as the creator of one or more courses. Please reassign or delete the courses first."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        private void DeleteUserImage(string? imageName)
        {
            if (string.IsNullOrEmpty(imageName)) return;

            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "users", imageName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error deleting image file: {ex.Message}");
            }
        }
    }
}
