// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using CB_Zad1.Models;
using CB_Zad1.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

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

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        [TempData]
        public string ChallengeX { get; set; }

        public int ChallengeA { get; set; } = 6;

        public class InputModel
        {
            [Required(ErrorMessage = "Pole 'Login' jest wymagane.")]
            public string Login { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }

            [Display(Name = "Wynik funkcji jednokierunkowej")]
            [Required(ErrorMessage = "Musisz podaæ wynik obliczeñ.")]
            public string MathResult { get; set; }
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

            var rnd = new Random();
            var x = rnd.Next(1, 100);
            ChallengeX = x.ToString();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            var invalidLoginMessage = "Login lub Has³o niepoprawny";

            // --- KROK 1: Weryfikacja Funkcji Jednokierunkowej ---
            // Sprawdzamy matematykê ZANIM sprawdzimy has³o (ochrona przed obci¹¿eniem bazy sprawdzaniem hase³)
            if (int.TryParse(ChallengeX, out int x))
            {
                // Wzór: y = exp(-a * x)
                // Uwaga: dla a=6 i x>1 wynik jest bardzo bliski zeru.
                var expectedValue = Math.Exp(-ChallengeA * x);

                // Próba parsowania tego, co wpisa³ u¿ytkownik (obs³uga kropki i przecinka)
                if (double.TryParse(Input.MathResult?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double userValue))
                {
                    // Porównanie z tolerancj¹ b³êdu (epsilon), bo to liczby zmiennoprzecinkowe
                    if (Math.Abs(userValue - expectedValue) > 0.00001)
                    {
                        ModelState.AddModelError(string.Empty, "B³êdny wynik funkcji matematycznej.");
                        await LogActivity(Input.Login, "Logowanie", "B³¹d: Z³y wynik funkcji (weryfikacja botów)");
                        return Page();
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Format liczby jest nieprawid³owy.");
                    return Page();
                }
            }
            // ---------------------------------------------------

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(Input.Login);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, invalidLoginMessage);
                    // Logujemy próbê logowania na nieistniej¹ce konto
                    await LogActivity(Input.Login, "Logowanie", "B³¹d: U¿ytkownik nie istnieje");
                    return Page();
                }

                // Twoja rêczna blokada (zostawiamy, jeœli u¿ywasz pola IsBlocked)
                if (user.IsBlocked)
                {
                    _logger.LogWarning($"Próba logowania na zablokowane konto: {user.UserName}");
                    ModelState.AddModelError(string.Empty, "Konto jest zablokowane przez administratora.");
                    await LogActivity(user.UserName, "Logowanie", "B³¹d: Konto zablokowane rêcznie");
                    return Page();
                }

                // Standardowe logowanie Identity
                // WA¯NE: Zmieniono lockoutOnFailure na TRUE (wymóg zadania dot. limitu prób)
                var result = await _signInManager.PasswordSignInAsync(
                    user,
                    Input.Password,
                    Input.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    // LOGOWANIE SUKCESU
                    await LogActivity(user.UserName, "Logowanie", "Sukces");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    // LOGOWANIE BLOKADY CZASOWEJ
                    await LogActivity(user.UserName, "Logowanie", "B³¹d: Tymczasowa blokada (zbyt wiele prób)");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, invalidLoginMessage);
                    // LOGOWANIE B£ÊDU HAS£A
                    await LogActivity(user.UserName, "Logowanie", "B³¹d: Nieprawid³owe has³o");
                    return Page();
                }
            }

            // Jeœli model nie jest valid, odœwie¿amy X, ¿eby u¿ytkownik móg³ spróbowaæ jeszcze raz
            Random rnd = new Random();
            ChallengeX = rnd.Next(1, 100).ToString();

            return Page();
        }

        // Metoda pomocnicza do zapisu w bazie
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
                // W razie b³êdu bazy nie chcemy wywaliæ ca³ej aplikacji, tylko zalogowaæ b³¹d w konsoli
                _logger.LogError($"Nie uda³o siê zapisaæ logu aktywnoœci: {ex.Message}");
            }
        }
    }
}