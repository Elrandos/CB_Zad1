using CB_Zad1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace CB_Zad1.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class AddUserModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly ILogger<AddUserModel> _logger;

        public AddUserModel(
            UserManager<User> userManager,
            IUserStore<User> userStore,
            ILogger<AddUserModel> logger)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Pe³na nazwa")]
            public string FullName { get; set; }

            [Required]
            [Display(Name = "Login (UserName)")]
            public string Login { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "{0} musi mieæ co najmniej {2} i co najwy¿ej {1} znaków.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Has³o")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "PotwierdŸ has³o")]
            [Compare("Password", ErrorMessage = "Has³o i jego potwierdzenie nie s¹ zgodne.")]
            public string ConfirmPassword { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (ModelState.IsValid)
            {
                var user = CreateUser();

                user.FullName = Input.FullName;

                await _userStore.SetUserNameAsync(user, Input.Login, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Administrator utworzy³ nowe konto z has³em.");

                    await _userManager.AddToRoleAsync(user, "User");

                    user.MustChangePasswordOnNextLogin = true;
                    await _userManager.UpdateAsync(user);

                    return RedirectToPage("./UserList");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private User CreateUser()
        {
            try
            {
                return Activator.CreateInstance<User>();
            }
            catch
            {
                throw new InvalidOperationException($"Nie mo¿na utworzyæ instancji '{nameof(User)}'. " +
                    $"Upewnij siê, ¿e '{nameof(User)}' nie jest klas¹ abstrakcyjn¹ i ma konstruktor bezparametrowy.");
            }
        }

        private IUserEmailStore<User> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("UI wymaga magazynu u¿ytkowników z obs³ug¹ poczty e-mail.");
            }
            return (IUserEmailStore<User>)_userStore;
        }
    }
}