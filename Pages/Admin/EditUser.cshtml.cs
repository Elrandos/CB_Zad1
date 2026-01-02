using CB_Zad1.Models;
using CB_Zad1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CB_Zad1.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EditUserModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ActivityLogger _logger;

        public EditUserModel(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ActivityLogger logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string GeneratedPasswordMessage { get; set; }

        public SelectList Roles { get; set; }

        public class InputModel
        {
            public string UserId { get; set; }

            [Display(Name = "Login")]
            public string UserName { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Display(Name = "Rola w systemie")]
            public string SelectedRole { get; set; }

            [Display(Name = "Konto zablokowane")]
            public bool IsBlocked { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var currentRole = userRoles.FirstOrDefault();

            Input = new InputModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                IsBlocked = user.IsBlocked,
                SelectedRole = currentRole
            };

            Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");

            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByIdAsync(Input.UserId);
            if (user == null) return NotFound();

            user.Email = Input.Email;

            if (Input.IsBlocked && !await _userManager.IsLockedOutAsync(user))
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            else if (!Input.IsBlocked && await _userManager.IsLockedOutAsync(user))
                await _userManager.SetLockoutEndDateAsync(user, null);


            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _logger.LogAsync(User.Identity.Name, "Edycja U¿ytkownika", $"Zaktualizowano dane dla: {user.UserName}");

                var currentRoles = await _userManager.GetRolesAsync(user);
                var currentRole = currentRoles.FirstOrDefault();

                if (Input.SelectedRole != currentRole)
                {
                    if (!string.IsNullOrEmpty(currentRole))
                        await _userManager.RemoveFromRoleAsync(user, currentRole);

                    if (!string.IsNullOrEmpty(Input.SelectedRole))
                        await _userManager.AddToRoleAsync(user, Input.SelectedRole);

                    await _logger.LogAsync(User.Identity.Name, "Zmiana Uprawnieñ", $"Zmiana roli z '{currentRole}' na '{Input.SelectedRole}' dla {user.UserName}");
                }

                return RedirectToPage("./UserList");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }

        public async Task<IActionResult> OnPostGenerateOneTimePasswordAsync()
        {
            var user = await _userManager.FindByIdAsync(Input.UserId);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var newPassword = "H1" + Guid.NewGuid().ToString().Substring(0, 6) + "!";

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                GeneratedPasswordMessage = $"SUKCES! Nowe has³o jednorazowe to: {newPassword}";

                await _logger.LogAsync(User.Identity.Name, "Reset Has³a", $"Wygenerowano has³o jednorazowe dla {user.UserName}");
            }
            else
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return await OnGetAsync(Input.UserId);
            }

            return RedirectToPage(new { id = Input.UserId });
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var user = await _userManager.FindByIdAsync(Input.UserId);
            if (user == null) return NotFound();

            var userName = user.UserName;
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                await _logger.LogAsync(User.Identity.Name, "Usuniêcie U¿ytkownika", $"Usuniêto konto: {userName}");
                return RedirectToPage("./UserList");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return await OnGetAsync(Input.UserId);
        }
    }
}