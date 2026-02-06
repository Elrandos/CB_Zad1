using System.ComponentModel.DataAnnotations;
using CB_Zad1.Models;
using CB_Zad1.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CB_Zad1.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ApplicationDbContext _context;

        public LoginModel(SignInManager<User> signInManager,
            UserManager<User> userManager,
            ILogger<LoginModel> logger,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string CaptchaQuestion { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Pole 'Login' jest wymagane.")]
            public string Login { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }

            [Required(ErrorMessage = "OdpowiedŸ na pytanie jest wymagana.")]
            [Display(Name = "OdpowiedŸ")]
            public string CaptchaAnswer { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;

            var questions = new Dictionary<string, string>
            {
                { "Co jest stolic¹ Wielkiej Brytanii?", "Londyn" }
            };

            var random = new Random();
            var selectedQuestion = questions.ElementAt(random.Next(questions.Count));

            CaptchaQuestion = selectedQuestion.Key;
            TempData["ExpectedCaptchaAnswer"] = selectedQuestion.Value;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (Input.Login == "admin" && Input.Password == "admin123")
            {
                await HandleCanaryToken();
                ModelState.AddModelError(string.Empty, "Nieudana próba logowania.");
                return Page();
            }

            var expectedAnswer = TempData["ExpectedCaptchaAnswer"] as string;
            if (string.IsNullOrWhiteSpace(Input.CaptchaAnswer) ||
                !string.Equals(Input.CaptchaAnswer.Trim(), expectedAnswer, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Input.CaptchaAnswer", "B³êdna odpowiedŸ na pytanie weryfikacyjne.");
                CaptchaQuestion = "Co jest stolic¹ Wielkiej Brytanii?";
                return Page();
            }

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByNameAsync(Input.Login);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Login lub Has³o niepoprawny");
                await LogActivity(Input.Login, "Logowanie", "B³¹d: U¿ytkownik nie istnieje");
                return Page();
            }

            if (user.IsBlocked)
            {
                _logger.LogWarning($"Próba logowania na zablokowane konto: {user.UserName}");
                ModelState.AddModelError(string.Empty, "Konto jest zablokowane przez administratora.");
                await LogActivity(user.UserName, "Logowanie", "B³¹d: Konto zablokowane rêcznie");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await LogActivity(user.UserName, "Logowanie", "Sukces");
                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
            }

            if (result.IsLockedOut)
            {
                await LogActivity(user.UserName, "Logowanie", "B³¹d: Tymczasowa blokada (zbyt wiele prób)");
                return RedirectToPage("./Lockout");
            }

            ModelState.AddModelError(string.Empty, "Login lub Has³o niepoprawny");
            await LogActivity(user.UserName, "Logowanie", "B³¹d: Nieprawid³owe has³o");
            return Page();
        }

        private async Task HandleCanaryToken()
        {
            try
            {
                using var client = new HttpClient();
                await client.GetAsync("http://canarytokens.com/feedback/5h1nwzg5toqct4myu1vdvj2a5/contact.php");
                await LogActivity("AFERA", "Logowanie", "B³¹d: ALERT CANARY");
            }
            catch
            {
                await LogActivity("AFERA", "Logowanie", "B³¹d: Problem z canary");
            }
        }

        private async Task LogActivity(string userName, string action, string description)
        {
            try
            {
                var log = new UserActivity
                {
                    UserName = userName ?? "Nieznany",
                    Action = action,
                    Description = description,
                    ActivityDate = DateTime.Now,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };

                _context.UserActivities.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Nie uda³o siê zapisaæ logu aktywnoœci: {ex.Message}");
            }
        }
    }
}