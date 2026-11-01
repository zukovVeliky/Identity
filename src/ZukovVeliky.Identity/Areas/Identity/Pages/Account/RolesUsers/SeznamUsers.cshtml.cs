using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ZukovVeliky.Identity.Pages.Account.RolesUsers
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class SeznamUsersModel(UserManager<IdentityUser> users) : PageModel
    {
        public List<IdentityUser> Uzivatele { get; private set; } = [];

        public async Task OnGetAsync()
        {
            Uzivatele = await users.Users.OrderBy(x => x.Email).ToListAsync();
        }

        public string CodeIdUser(string id) => CodeDecode.Base64Encode(id);
    }
}
