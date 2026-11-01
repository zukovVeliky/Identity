using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ZukovVeliky.Identity.Pages.Account.RolesUsers
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class SeznamEditRoleModel(RoleManager<IdentityRole> roles) : PageModel
    {
        [TempData]
        public string? ErrorMessage { get; set; }

        public List<IdentityRole> Roles { get; private set; } = [];

        public static bool IsBuiltIn(string? name) => ZukovVeliky.Identity.Roles.BuiltIn.Contains(name);

        public async Task OnGetAsync()
        {
            Roles = await roles.Roles.OrderBy(x => x.Name).ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateRoleAsync(string? textRole)
        {
            var name = (textRole ?? string.Empty).Trim();
            if (name.Length > 0 && !await roles.RoleExistsAsync(name))
            {
                await roles.CreateAsync(new IdentityRole(name));
            }

            return RedirectToPage();
        }

        /// <summary>Vestavěné role aplikace potřebuje, proto je smazat nejde.</summary>
        public async Task<IActionResult> OnPostDeleteAsync(string? id)
        {
            if (!string.IsNullOrWhiteSpace(id) && await roles.FindByIdAsync(id) is { } role)
            {
                if (IsBuiltIn(role.Name))
                {
                    ErrorMessage = $"Roli {role.Name} nelze odstranit, aplikace ji potřebuje.";
                }
                else
                {
                    await roles.DeleteAsync(role);
                }
            }

            return RedirectToPage();
        }
    }
}
