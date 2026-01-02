using CB_Zad1.Data;
using CB_Zad1.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; // Potrzebne do IP

namespace CB_Zad1.Services
{
    public class ActivityLogger
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogger(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string userName, string action, string description)
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new SystemSettings { IsLoggingEnabled = true };
                _context.SystemSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            if (!settings.IsLoggingEnabled)
            {
                return;
            }

            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            // Jeśli IP jest puste (np. localhost ::1 lub błąd), wpisz wartość domyślną, 
            // żeby nie wywaliło błędu NOT NULL
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = "Lokalny/System";
            }

            // 3. Tworzenie logu
            var log = new UserActivity
            {
                UserName = userName ?? "System",
                Action = action,
                Description = description,
                ActivityDate = DateTime.Now,
                IpAddress = ipAddress // <-- TUTAJ BYŁ BŁĄD (brakowało tego przypisania)
            };

            _context.UserActivities.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}