using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ZukovVeliky.Identity.Pages.Account.RolesUsers
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class UserEditRoleModel(UserManager<IdentityUser> users, RoleManager<IdentityRole> roles) : PageModel
    {
        private IList<string> _userRoles = [];

        [TempData]
        public string? ErrorMessage { get; set; }

        public string CIP { get; private set; } = string.Empty;
        public List<IdentityRole> Roles { get; private set; } = [];
        public IdentityUser Uzivatel { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync(string? cip)
            => await LoadUserAsync(cip) ? Page() : RedirectToPage("./SeznamUsers");

        /// <summary>Přidá uživatele do role.</summary>
        public async Task<IActionResult> OnPostAddAsync(string id, string cip)
        {
            if (await LoadUserAsync(cip) && await RoleNameAsync(id) is { } role)
            {
                await users.AddToRoleAsync(Uzivatel, role);
            }

            return RedirectToPage(new { cip });
        }

        /// <summary>Odebere uživatele z role. Poslední administrátor roli neztratí, jinak by správa zůstala bez přístupu.</summary>
        public async Task<IActionResult> OnPostRemoveAsync(string id, string cip)
        {
            if (await LoadUserAsync(cip) && await RoleNameAsync(id) is { } role)
            {
                if (role == ZukovVeliky.Identity.Roles.Administrator && (await users.GetUsersInRoleAsync(role)).Count <= 1)
                {
                    ErrorMessage = "Posledního administrátora nelze odebrat.";
                }
                else
                {
                    await users.RemoveFromRoleAsync(Uzivatel, role);
                }
            }

            return RedirectToPage(new { cip });
        }

        public string Zakodovani(string arg) => CodeDecode.Base64Encode(arg);

        public bool UserInRole(string roleName) => _userRoles.Contains(roleName);

        private async Task<string?> RoleNameAsync(string encodedId)
            => (await roles.FindByIdAsync(CodeDecode.Base64Decode(encodedId)))?.Name;

        private async Task<bool> LoadUserAsync(string? cip)
        {
            Roles = await roles.Roles.OrderBy(x => x.Name).ToListAsync();
            if (string.IsNullOrWhiteSpace(cip) || await users.FindByIdAsync(CodeDecode.Base64Decode(cip)) is not { } user)
            {
                return false;
            }

            CIP = cip;
            Uzivatel = user;
            _userRoles = await users.GetRolesAsync(user);
            return true;
        }
    }
}
