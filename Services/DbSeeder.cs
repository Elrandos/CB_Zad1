using CB_Zad1.Models;
using Microsoft.AspNetCore.Identity;

namespace CB_Zad1.Services
{
    public class DbSeeder
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DbSeeder(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }

            if (await _userManager.FindByNameAsync("ADMIN") == null)
            {
                var adminUser = new User
                {
                    UserName = "ADMIN",
                    Email = "admin@admin.pl",
                    FullName = "Administrator Systemu",
                    EmailConfirmed = true,
                    MustChangePasswordOnNextLogin = true
                };

                var result = await _userManager.CreateAsync(adminUser, "Admin1234!");

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
