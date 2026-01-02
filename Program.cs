using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CB_Zad1.Data;
using CB_Zad1.Models;
using CB_Zad1.Services; // Wa¿ne: namespace Twojego serwisu
using CB_Zad1.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// 1. Konfiguracja Bazy Danych
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 2. Konfiguracja Identity (U¿ytkownicy, Has³a i BLOKADY)
builder.Services.AddDefaultIdentity<User>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    // Ustawienia has³a
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // --- BLOKADA KONTA (Zadanie - Rys. 4) ---
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// --- SESJA (Zadanie - Rys. 5) ---
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    options.SlidingExpiration = true;
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.LogoutPath = "/Identity/Account/Logout";
});

// 3. Rejestracja W£ASNYCH Serwisów
// TO JEST LINIA, KTÓREJ BRAKOWA£O I POWODOWA£A B£¥D:
builder.Services.AddScoped<ActivityLogger>();

builder.Services.AddTransient<DbSeeder>();
builder.Services.AddScoped<CheckPasswordFilter>();

// 4. Polityki autoryzacji
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// 5. Konfiguracja Razor Pages
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "Admin");

    options.Conventions.AddFolderApplicationModelConvention("/", model =>
    {
        model.Filters.Add(new ServiceFilterAttribute(typeof(CheckPasswordFilter)));
    });
});

var app = builder.Build();

// 6. Seeding bazy danych (Inicjalizacja)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var seeder = services.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}

// 7. Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();