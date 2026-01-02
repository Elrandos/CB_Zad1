using CB_Zad1.Data;
using CB_Zad1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CB_Zad1.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ActivityLogModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<UserActivity> Activities { get; set; }

        [BindProperty]
        public bool IsLoggingEnabled { get; set; }

        public async Task OnGetAsync()
        {
            Activities = await _context.UserActivities
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            IsLoggingEnabled = settings?.IsLoggingEnabled ?? true;
        }

        public async Task<IActionResult> OnPostUpdateSettingsAsync()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new SystemSettings();
                _context.SystemSettings.Add(settings);
            }

            settings.IsLoggingEnabled = IsLoggingEnabled;

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteLogsAsync()
        {
            var allLogs = _context.UserActivities.ToList();
            _context.UserActivities.RemoveRange(allLogs);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}
