using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace ZukovVeliky.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IdentityKitOptions _kit;

        public ResendEmailConfirmationModel(UserManager<IdentityUser> userManager, IEmailSender emailSender, Microsoft.Extensions.Options.IOptions<IdentityKitOptions> kit)
        {
            _kit = kit.Value;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Same answer whether or not the address exists, so the page does not reveal accounts.
            const string sentMessage = "Pokud u nás účet s touto adresou existuje, poslali jsme na ni nový potvrzovací odkaz.";

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null || await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty, sentMessage);
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = code },
                protocol: Request.Scheme);
            try
            {
                await _emailSender.SendEmailAsync(
                    Input.Email,
                    $"{_kit.SiteName} – potvrzení e-mailu",
                    $"<p>Dobrý den,</p><p>účet na webu {HtmlEncoder.Default.Encode(_kit.SiteName)} aktivujete kliknutím na tento odkaz:</p>" +
                    $"<p><a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>Potvrdit e-mail</a></p>" +
                    "<p>Pokud jste o odkaz nežádali, tento e-mail ignorujte.</p>");
            }
            catch (Exception)
            {
                // EmailSender already logs the failure.
                ModelState.AddModelError(string.Empty, "E-mail se teď nepodařilo odeslat. Zkuste to prosím za chvíli znovu.");
                return Page();
            }

            ModelState.AddModelError(string.Empty, sentMessage);
            return Page();
        }
    }
}
