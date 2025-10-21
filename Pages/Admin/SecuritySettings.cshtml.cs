using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace CB_Zad1.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class SecuritySettingsModel : PageModel
    {
        private readonly IdentityOptions _identityOptions;

        public SecurityPolicyViewModel Policy { get; set; }

        public SecuritySettingsModel(IOptions<IdentityOptions> identityOptions)
        {
            _identityOptions = identityOptions.Value;
        }

        public void OnGet()
        {
            Policy = new SecurityPolicyViewModel
            {
                RequiredLength = _identityOptions.Password.RequiredLength,
                RequireDigit = _identityOptions.Password.RequireDigit,
                RequireLowercase = _identityOptions.Password.RequireLowercase,
                RequireUppercase = _identityOptions.Password.RequireUppercase,
                RequireNonAlphanumeric = _identityOptions.Password.RequireNonAlphanumeric
            };
        }

        public class SecurityPolicyViewModel
        {
            public int RequiredLength { get; set; }
            public bool RequireDigit { get; set; }
            public bool RequireLowercase { get; set; }
            public bool RequireUppercase { get; set; }
            public bool RequireNonAlphanumeric { get; set; }
        }
    }
}