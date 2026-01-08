using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels.Instructor;
using Models.ViewModels.Student;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ELClass.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StudentController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly UserManager<ApplicationUser> userManager;

        public StudentController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
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
            var student = await unitOfWork.StudentRepository.GetOneAsync(e => e.Id == id, include: e => e.Include(e => e.ApplicationUser));
            if (student == null)
            {
                return View("AdminNotFoundPage");
            }

            var courses = await unitOfWork.StudentCourseRepository.GetAsync(filter: e => e.StudentId == id, include: e => e.Include(e => e.Course));
            var instructors = await unitOfWork.InstructorStudentRepository.GetAsync(filter: e => e.StudentId == id, include: e => e.Include(e => e.Instructor));
            var model = new StudentDetailsVM()
            {
                Student = student,
                StudentCourses = courses.ToList() ?? new List<StudentCourse>(),
                InstructorStudents = instructors.ToList() ?? new List<InstructorStudent>()
            };
            return View(model);
        }

        public async Task<IActionResult> AssignCourse(int courseId, string studentId)
        {
            if (courseId != 0 && studentId != null)
            {
                var studentCourse = new StudentCourse
                {
                    CourseId = courseId,
                    StudentId = studentId,
                    CreatedAt = DateTime.Now,
                    CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier)
                };
                var res = await unitOfWork.StudentCourseRepository.CreateAsync(studentCourse);

                if (res)
                {
                    var succ = await unitOfWork.CommitAsync();
                    if (succ)
                    {
                        return Json(new { success = true });
                    }
                }
                return BadRequest();
            }

            return View("AdminNotFoundPage");
        }

        public async Task<IActionResult> AssignInstructor(string studentId, string instructorId)
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
                var res = await unitOfWork.InstructorStudentRepository.CreateAsync(instructorStudent);
                if (res)
                {
                    var succ = await unitOfWork.CommitAsync();
                    if (succ)
                    {
                        return Json(new { success = true });
                    }
                }
                return BadRequest();
            }
            return View("AdminNotFoundPage");
        }

        [HttpPost]

        public async Task<IActionResult> GetStudents()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = int.Parse(Request.Form["start"].FirstOrDefault() ?? "0");
                var length = int.Parse(Request.Form["length"].FirstOrDefault() ?? "10");
                var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";



                var recordsTotal = await unitOfWork.StudentRepository.CountAsync();



                var students = await unitOfWork.StudentRepository.GetAsync(
                    filter: s => string.IsNullOrEmpty(searchValue) ||
                                 s.NameEn.Contains(searchValue) ||
                                 s.NameAr.Contains(searchValue),
                    skip: start,
                    take: length,
                    orderBy: q => q.OrderBy(s => s.NameEn)
                );

                var lang = HttpContext.Session.GetString("Language") ?? "en";

                var data = students.Select(c => new
                {
                    id = c.Id,
                    name = lang == "en" ? c.NameEn : c.NameAr,
                }).ToList();


                var recordsFiltered = string.IsNullOrEmpty(searchValue)
                    ? recordsTotal
                    : (await unitOfWork.StudentRepository.CountAsync(filter: s => s.NameEn.Contains(searchValue) || s.NameAr.Contains(searchValue)));

                return Json(new
                {
                    draw = draw,
                    recordsTotal = recordsTotal,
                    recordsFiltered = recordsFiltered,
                    data = data
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetStudentCourses()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = int.Parse(Request.Form["start"].FirstOrDefault() ?? "0");
                var length = int.Parse(Request.Form["length"].FirstOrDefault() ?? "10");
                var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";
                var studentId = Request.Form["studentId"].FirstOrDefault();

                
                Expression<Func<StudentCourse, bool>> filter = e =>
                    e.StudentId == studentId &&
                    (string.IsNullOrEmpty(searchValue) || e.Course.TitleEn.Contains(searchValue) || e.Course.TitleAr.Contains(searchValue));

                
                var courses = await unitOfWork.StudentCourseRepository.GetAsync(
                    filter: filter,
                    include: e => e.Include(x => x.Course),
                    orderBy: q => q.OrderByDescending(x => x.CreatedAt ?? DateTime.MinValue),
                    skip: start,
                    take: length
                );

                
                
                var recordsFiltered = await unitOfWork.StudentCourseRepository.CountAsync(filter: filter);


                var result = courses.Select(c => new
                {
                    courseId = c.CourseId,
                    title = c.Course.TitleEn 
                }).ToList();

                return Json(new
                {
                    draw,
                    recordsTotal = recordsFiltered, 
                    recordsFiltered = recordsFiltered,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetStudentInstructors()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = int.Parse(Request.Form["start"].FirstOrDefault() ?? "0");
                var length = int.Parse(Request.Form["length"].FirstOrDefault() ?? "10");
                var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";
                var studentId = Request.Form["studentId"].FirstOrDefault();

                
                Expression<Func<InstructorStudent, bool>> filter = e =>
                    e.StudentId == studentId &&
                    (string.IsNullOrEmpty(searchValue) || e.Instructor.NameEn.Contains(searchValue) || e.Instructor.NameAr.Contains(searchValue));

                
                var instructors = await unitOfWork.InstructorStudentRepository.GetAsync(
                    filter: filter,
                    include: e => e.Include(x => x.Instructor),
                    orderBy: q => q.OrderByDescending(x => x.CreatedAt ?? DateTime.MinValue),
                    skip: start,
                    take: length
                );

                
               
                var recordsFiltered = await unitOfWork.InstructorStudentRepository.CountAsync(filter: filter);


                var result = instructors.Select(i => new
                {
                    instructorId = i.InstructorId,
                    name = i.Instructor.NameEn 
                }).ToList();

                return Json(new
                {
                    draw,
                    recordsTotal = recordsFiltered, 
                    recordsFiltered = recordsFiltered,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }



        public async Task<IActionResult> RemoveCourse(int courseId, string studentId)
        {
            var course = await unitOfWork.StudentCourseRepository.GetOneAsync(filter: e => e.StudentId == studentId && e.CourseId == courseId);
            if (course == null)
            {
                return View("AdminNotFoundPage");
            }

            var result = await unitOfWork.StudentCourseRepository.DeleteAsync(course);
            if (result)
            {
                var suc = await unitOfWork.CommitAsync();
                if (suc)
                {
                    return Json(new { success = true });
                }

            }
            return BadRequest();
        }

        public async Task<IActionResult> RemoveInstructor(string stdId, string insId)
        {
            var instructor = await unitOfWork.InstructorStudentRepository.GetOneAsync(filter: e => e.InstructorId == insId && e.StudentId == stdId);
            if (instructor == null)
            {
                return View("AdminNotFoundPage");
            }

            var result = await unitOfWork.InstructorStudentRepository.DeleteAsync(instructor);
            if (result)
            {
                var suc = await unitOfWork.CommitAsync();
                if (suc)
                {

                    return Json(new { success = true });
                }

            }
            return BadRequest();
        }


        public async Task<IActionResult> SearchInstructors(string term, string studentId)
        {
            var instructors = await unitOfWork.InstructorRepository.GetAsync(filter: e =>
            (e.NameAr.Contains(term) || e.NameEn.Contains(term)));


            var insStdunt = await unitOfWork.InstructorStudentRepository.GetAsync(filter: e => e.StudentId == studentId);
            if (insStdunt.Any())
            {
                var assignedStudentIds = insStdunt.Select(ic => ic.InstructorId).ToList();
                instructors = instructors.Where(c => !assignedStudentIds.Contains(c.Id)).ToList();
            }

            var result = instructors
                .Select(c => new
                {
                    id = c.Id,
                    text = $"{c.NameEn} - {c.NameAr}"
                });

            return Json(result);
        }
        public async Task<IActionResult> SearchCourses(string term, string stdId)
        {
            var courses = await unitOfWork.CourseRepository.GetAsync(filter: e =>
            (e.TitleAr.Contains(term) || e.TitleEn.Contains(term)));


            var stdCourses = await unitOfWork.StudentCourseRepository.GetAsync(filter: e => e.StudentId == stdId);
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
        public IActionResult Create()
        {
            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        
        public async Task<IActionResult> Create(Student std, string Password, string ConfirmPassword)
        {
            
            ModelState.Remove("ApplicationUser.Instructor");
            ModelState.Remove("ApplicationUser.Student");
            

            if (!ModelState.IsValid)
            {
                return View(std);
            }

           
            if (Password != ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
                return View(std);
            }

            try
            {
                var email = std.ApplicationUser.Email;
                var userName = std.ApplicationUser.Email!.Split('@')[0] + new Random().Next(10, 99);

               
                var user = new ApplicationUser
                {
                    UserName = userName,
                    Email = email,
                    PhoneNumber = std.ApplicationUser.PhoneNumber,
                    AddressEN = std.ApplicationUser.AddressEN,
                    AddressAR = std.ApplicationUser.AddressAR,
                    EmailConfirmed = true
                };

                
                var result = await userManager.CreateAsync(user, Password);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);

                    TempData["Error"] = "Failed to create user account";
                    return View(std);
                }

                
                await userManager.AddToRoleAsync(user, "Student");

                
                std.Id = user.Id;
                
                std.ApplicationUser = null!;

                await unitOfWork.StudentRepository.CreateAsync(std);
                await unitOfWork.CommitAsync();

                TempData["Success"] = "Student account has been created successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred: " + ex.Message;
                return View(std);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudentDetails(StudentDetailsVM model)
        {
            var std = model.Student;


            ModelState.Remove("Student.ApplicationUser");
            ModelState.Remove("Student.ApplicationUser.Student");
            ModelState.Remove("Student.ApplicationUser.instructor");
            ModelState.Remove("StudentCourses");
            ModelState.Remove("InstructorStudents");

            if (ModelState.IsValid)
            {
                try
                {

                    var studentInDb = await unitOfWork.StudentRepository.GetOneAsync(
                        e => e.Id == std.Id,
                        include: query => query.Include(e => e.ApplicationUser)
                    );

                    if (studentInDb == null)
                    {
                        return NotFound();
                    }


                    studentInDb.NameEn = std.NameEn;
                    studentInDb.NameAr = std.NameAr;


                    if (studentInDb.ApplicationUser != null && std.ApplicationUser != null)
                    {
                        var user = studentInDb.ApplicationUser;
                        user.Email = std.ApplicationUser.Email;
                       
                        user.PhoneNumber = std.ApplicationUser.PhoneNumber;
                        user.AddressEN = std.ApplicationUser.AddressEN;
                        user.AddressAR = std.ApplicationUser.AddressAR;


                        await userManager.UpdateNormalizedEmailAsync(user);
                        
                    }

                    await unitOfWork.CommitAsync();

                    TempData["Success"] = "Student updated successfully";
                    return RedirectToAction("Details", new { id = std.Id });
                }
                catch (Exception ex)
                {
                    TempData["error"] = "Error: " + ex.Message;
                }
            }


            var courses = await unitOfWork.StudentCourseRepository.GetAsync(e => e.StudentId == std.Id, include: i => i.Include(c => c.Course));

            model.StudentCourses = courses.ToList();

            if (TempData["error"] is null)
            {
                TempData["error"] = "Please correct the errors and try again.";
            }

            return View("Details", model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeProfilePhoto(string StudentId, IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                TempData["Error"] = "Please select a valid image.";
                return RedirectToAction("Details", new { id = StudentId });
            }

            try
            {

                var student = await unitOfWork.StudentRepository.GetOneAsync(
                    u => u.Id == StudentId,
                    include: e => e.Include(e => e.ApplicationUser));

                if (student == null || student.ApplicationUser == null)
                {
                    TempData["Error"] = "Student not found.";
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


                if (!string.IsNullOrEmpty(student.ApplicationUser.Img))
                {
                    var oldPath = Path.Combine(folderPath, student.ApplicationUser.Img);
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }


                student.ApplicationUser.Img = fileName;

                await unitOfWork.CommitAsync();

                TempData["Success"] = "Profile photo updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred: " + ex.Message;
            }

            return RedirectToAction("Details", new { id = StudentId });
        }

        [HttpPost] 
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
           
            var student = await unitOfWork.StudentRepository.GetOneAsync(
                c => c.Id == id,
                include: i => i.Include(u => u.ApplicationUser));

            if (student == null)
            {
                return View("AdminNotFoundPage");
            }

            var user = student.ApplicationUser;

            try
            {
               
                var studentCourses = await unitOfWork.StudentCourseRepository.GetAsync(sc => sc.StudentId == id);
                foreach (var sc in studentCourses)
                {
                    await unitOfWork.StudentCourseRepository.DeleteAsync(sc);
                }

                
                var instructorStudents = await unitOfWork.InstructorStudentRepository.GetAsync(isd => isd.StudentId == id);
                foreach (var isd in instructorStudents)
                {
                    await unitOfWork.InstructorStudentRepository.DeleteAsync(isd);
                }

                
                if (user != null)
                {
                    DeleteUserImage(user.Img);
                }

                
                await unitOfWork.StudentRepository.DeleteAsync(student);

                
                await unitOfWork.CommitAsync();

                
                if (user != null)
                {
                    var result = await userManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        TempData["Error"] = "Student profile deleted, but user account removal failed.";
                        return RedirectToAction("Index");
                    }
                }

                TempData["Success"] = "Student and all related data (courses/instructors) deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An unexpected error occurred: " + ex.Message;
            }

            return RedirectToAction("Index");
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

