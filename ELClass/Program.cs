// ... الـ Usings زي ما هي

using DataAccess;
using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using ELClass.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Utilites;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(); // شغال تمام

// 2. تعريف الـ DbContext (مرة واحدة فقط!)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
      options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(config =>
{
    config.Password.RequiredLength = 8;
    config.User.RequireUniqueEmail = true;
    config.SignIn.RequireConfirmedEmail = false;
    config.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    config.Lockout.MaxFailedAccessAttempts = 5;
    config.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4. Dependency Injection
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// ملحوظة: لو الـ UnitOfWork جواها الـ Repositories دي، مش محتاج تسجلهم هنا تاني
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddSignalR();
//builder.Services.AddSingleton<IUserIdProvider, NameUserIdProvider>();
var app = builder.Build();

// 5. Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(); // سكالر بيشتغل في الـ Dev بس أحسن
}
else
{
    app.UseExceptionHandler("/Home/Error");
   app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles(); // تأكد إنها قبل الـ Routing
app.UseRouting();

app.UseSession();
app.UseAuthentication(); // مهم جداً تضيف دي عشان الشات يعرف مين اللي باعت!
app.UseAuthorization();

// 6. Routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 7. Seed Roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roleNames = { "Admin", "Teacher", "Student" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}
//signalR

app.MapHub<ChatHub>("/chatHub");






app.Run();