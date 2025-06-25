// Program.cs - Updated routing configuration
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Hubs;
using LibraryManagementSystem.Data.Seed;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Entity Framework with SQL Server
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services with UI support
builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<LibraryContext>();

// Configure application cookie to redirect to login
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// Add SignalR
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

//  Default route configuration
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add a specific route for root URL to redirect to login
app.MapGet("/", context =>
{
    if (!context.User.Identity.IsAuthenticated)
    {
        context.Response.Redirect("/Identity/Account/Login");
    }
    else if (context.User.IsInRole("Bibliotecário"))
    {
        context.Response.Redirect("/Admin");
    }
    else
    {
        context.Response.Redirect("/Home");
    }
    return Task.CompletedTask;
});

app.MapRazorPages();
app.MapHub<LibraryHub>("/libraryHub");

app.Run();
