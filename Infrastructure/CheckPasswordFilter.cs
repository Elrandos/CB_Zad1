using CB_Zad1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CB_Zad1.Infrastructure
{
    public class CheckPasswordFilter : IAsyncPageFilter
    {
        private readonly UserManager<User> _userManager;

        public CheckPasswordFilter(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var pagePath = context.ActionDescriptor.DisplayName;
                if (pagePath.Contains("/Account/Login") ||
                    pagePath.Contains("/Account/Logout") ||
                    pagePath.Contains("/Account/Manage/ChangePassword"))
                {
                    await next.Invoke();
                    return;
                }

                var user = await _userManager.GetUserAsync(context.HttpContext.User);
                if (user != null)
                {
                    var mustChange = user.MustChangePasswordOnNextLogin || user.PasswordExpirationDate.HasValue && user.PasswordExpirationDate.Value < DateTime.UtcNow;

                    if (mustChange)
                    {
                        context.Result = new RedirectToPageResult("/Account/Manage/ChangePassword",
                            new { area = "Identity", message = "Musisz ustawić nowe hasło." });
                        return;
                    }
                }
            }
            await next.Invoke();
        }

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;
    }
}
