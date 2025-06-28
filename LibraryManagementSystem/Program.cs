using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Hubs;
using LibraryManagementSystem.Data.Seed;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add Entity Framework with SQL Server
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<LibraryContext>()
.AddDefaultUI();

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

// FIXED: Add JWT Authentication BEFORE other services
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "LibraryManagementSystem",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "LibraryManagementSystem",
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.ASCII.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? "MinhaChaveSecretaSuperSeguraParaJWT123456789"))
    };
});

// Add SignalR
builder.Services.AddSignalR();

// Add API Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sistema de Gestão de Biblioteca API",
        Version = "v1",
        Description = "API para Sistema de Gestão de Biblioteca - Desenvolvimento Web - IPT",
        Contact = new OpenApiContact
        {
            Name = "Licenciatura em Engenharia Informática - IPT",
            Email = "dev@biblioteca.pt"
        },
        License = new OpenApiLicense
        {
            Name = "Instituto Politécnico de Tomar",
            Url = new Uri("https://www.ipt.pt")
        }
    });

    // FIXED: Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Exemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseItToSeedSqlServer();

    // Enable Swagger in development
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema de Gestão de Biblioteca API V1");
        c.RoutePrefix = "api-docs"; // Access via /api-docs
        c.DocumentTitle = "Sistema de Gestão de Biblioteca - API Documentation";
        c.DefaultModelsExpandDepth(-1); // Hide schemas section
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// FIXED: Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map API controllers
app.MapControllers();

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
app.MapHub<BookReviewHub>("/bookReviewHub");

app.Run();
