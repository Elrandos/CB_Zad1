using Microsoft.AspNetCore.Identity;

namespace CB_Zad1.Models
{
    public class User : IdentityUser
    {
        [PersonalData]
        public string? FullName { get; set; }

        public bool IsBlocked { get; set; }

        public bool PasswordRestrictionsEnabled { get; set; }

        public DateTime? PasswordExpirationDate { get; set; }

        public bool MustChangePasswordOnNextLogin { get; set; } = false;
    }
}
