using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Hubs;
using LibraryManagementSystem.Data.Seed;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add Entity Framework with SQL Server
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Default Identity with confirmation link on confirmation page
builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    // Email confirmation settings (shows link on confirmation page)
    options.SignIn.RequireConfirmedAccount = true;

    // Password requirements
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<LibraryContext>();

// Configure application cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

// Add SignalR (required by evaluation document)
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseItToSeedSqlServer();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Root URL routing
app.MapGet("/", async context =>
{
    if (!context.User.Identity.IsAuthenticated)
    {
        context.Response.Redirect("/Identity/Account/Login");
    }
    else if (context.User.IsInRole("Bibliotecário"))
    {
        context.Response.Redirect("/Admin");
    }
    else if (context.User.IsInRole("Membro"))
    {
        context.Response.Redirect("/Home");
    }
    else
    {
        context.Response.Redirect("/Identity/Account/AccessDenied");
    }

    await Task.CompletedTask;
});

app.MapRazorPages();
app.MapHub<LibraryHub>("/libraryHub");

app.Run();