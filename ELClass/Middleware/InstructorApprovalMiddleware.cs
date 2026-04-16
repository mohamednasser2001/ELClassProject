using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Identity;
using Models;

namespace ELClass.Middleware
{
    public class InstructorApprovalMiddleware
    {
        private readonly RequestDelegate _next;

        public InstructorApprovalMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // شغّل فقط على الـ Instructor area
            var isInstructorArea = path.StartsWith("/instructor");

            // استثني صفحة الانتظار نفسها + logout عشان ما تبقاش loop
            var isExcluded = path.Contains("/instructor/home/waiting")
                          || path.Contains("/account/logout")
                          || path.Contains("/identity");

            if (isInstructorArea && !isExcluded && context.User.Identity?.IsAuthenticated == true)
            {
                var userId = userManager.GetUserId(context.User);

                if (!string.IsNullOrEmpty(userId))
                {
                    var instructor = await unitOfWork.InstructorRepository
                        .GetOneAsync(i => i.Id == userId);

                    if (instructor != null && !instructor.IsApproved)
                    {
                        context.Response.Redirect("/Instructor/Home/Waiting");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
