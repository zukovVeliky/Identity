using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace ZukovVeliky.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "E-mail")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "{0} musí být nejméně {2} a maximálně {1} znaků dlouhé.", MinimumLength = 4)]
            [DataType(DataType.Password)]
            [Display(Name = "Heslo")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Ověření hesla")]
            [Compare("Password", ErrorMessage = "Heslo a ověřovací heslo se neshodují.")]
            public string ConfirmPassword { get; set; }
        }

        // Public registration is on unless IdentityKit:AllowPublicRegistration is false.
        // A new account has no role and must confirm its e-mail before signing in;
        // editor access is granted by an administrator.
        private IdentityKitOptions Kit
            => HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<IdentityKitOptions>>().Value;

        private bool RegistrationEnabled => Kit.AllowPublicRegistration;

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            if (!RegistrationEnabled)
            {
                return NotFound();
            }

            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (!RegistrationEnabled)
            {
                return NotFound();
            }

            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Uživatel vytvořil nový účet s heslem.");

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    var emailSent = true;
                    try
                    {
                        await _emailSender.SendEmailAsync(Input.Email, $"{Kit.SiteName} – potvrzení registrace",
                            $"<p>Dobrý den,</p><p>děkujeme za registraci na webu {HtmlEncoder.Default.Encode(Kit.SiteName)}. Účet aktivujete kliknutím na tento odkaz:</p>" +
                            $"<p><a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>Potvrdit e-mail</a></p>" +
                            "<p>Pokud jste se neregistrovali, tento e-mail ignorujte.</p>");
                    }
                    catch (Exception ex)
                    {
                        // The account exists; the user can ask for a new link on the resend page.
                        emailSent = false;
                        _logger.LogError(ex, "Potvrzovaci e-mail po registraci se nepodarilo odeslat.");
                    }

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl, emailSent = emailSent });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
