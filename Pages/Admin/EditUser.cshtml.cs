using CB_Zad1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace CB_Zad1.Pages.Admin
{
    public class EditUserModel : PageModel
    {
        private readonly UserManager<User> _userManager;

        public EditUserModel(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public User TargetUser { get; private set; }

        public class InputModel
        {
            public string UserId { get; set; }

            [Display(Name = "Pe³na nazwa")]
            public string FullName { get; set; }

            [Display(Name = "Konto zablokowane")]
            public bool IsBlocked { get; set; }

            [Display(Name = "Wymagaj silnego has³a")]
            public bool PasswordRestrictionsEnabled { get; set; }

            [Display(Name = "Wa¿noœæ has³a (w dniach)")]
            public int? PasswordExpiresInDays { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null) return NotFound();

            TargetUser = await _userManager.FindByIdAsync(id);
            if (TargetUser == null) return NotFound();

            Input = new InputModel
            {
                UserId = TargetUser.Id,
                FullName = TargetUser.FullName,
                IsBlocked = TargetUser.IsBlocked,
                PasswordRestrictionsEnabled = TargetUser.PasswordRestrictionsEnabled
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            TargetUser = await _userManager.FindByIdAsync(Input.UserId);
            if (TargetUser == null) return NotFound();

            TargetUser.FullName = Input.FullName;

            TargetUser.IsBlocked = Input.IsBlocked;

            TargetUser.PasswordRestrictionsEnabled = Input.PasswordRestrictionsEnabled;

            if (Input.PasswordExpiresInDays.HasValue)
            {
                TargetUser.PasswordExpirationDate = DateTime.UtcNow.AddDays(Input.PasswordExpiresInDays.Value);
            }
            else
            {
                TargetUser.PasswordExpirationDate = null;
            }

            var result = await _userManager.UpdateAsync(TargetUser);
            if (result.Succeeded)
            {
                return RedirectToPage("./UserList");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        public async Task<IActionResult> OnPostResetPasswordAsync()
        {
            TargetUser = await _userManager.FindByIdAsync(Input.UserId);
            if (TargetUser == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(TargetUser);

            var result = await _userManager.ResetPasswordAsync(TargetUser, token, "Temp123!");

            if (result.Succeeded)
            {
                TargetUser.MustChangePasswordOnNextLogin = true;
                await _userManager.UpdateAsync(TargetUser);
            }
            else
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }

            return RedirectToPage(new { id = TargetUser.Id });
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            TargetUser = await _userManager.FindByIdAsync(Input.UserId);
            if (TargetUser == null) return NotFound();

            var result = await _userManager.DeleteAsync(TargetUser);

            if (result.Succeeded)
            {
                return RedirectToPage("./UserList");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }
    }
}