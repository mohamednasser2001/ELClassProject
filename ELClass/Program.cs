using DataAccess;
using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using ELClass.Hubs; 
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Utilites;
using Scalar.AspNetCore;
using Utilities.DbIntializer;



var builder = WebApplication.CreateBuilder(args);

// =======================
// 1️⃣ Services
// =======================

// MVC
builder.Services.AddControllersWithViews();

// SignalR
builder.Services.AddSignalR();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Identity
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

// Cookie paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// Email
builder.Services.AddTransient<IEmailSender, EmailSender>();

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// =======================
// 2️⃣ Dependency Injection
// =======================
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IDbIntializer, DbIntializer>();

// =======================
// 3️⃣ Build App
// =======================
var app = builder.Build();

// =======================
// 4️⃣ Middleware
// =======================
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();


app.MapHub<ChatHub>("/chatHub");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");


app.MapControllerRoute(
    name: "default",
    pattern: "{area=admin}/{controller=Home}/{action=Index}/{id?}");



// =======================
// 7️⃣ Seed Roles + DB
// =======================
using (var scope = app.Services.CreateScope())
{
  

    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbIntializer>();
    dbInitializer.Initialize();
}

// =======================
app.Run();


