using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ZukovVeliky.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        public string Email { get; set; } = string.Empty;

        public bool EmailSent { get; set; } = true;

        public IActionResult OnGet(string? email, bool emailSent = true)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToPage("/Index");
            }

            Email = email;
            EmailSent = emailSent;
            return Page();
        }
    }
}
