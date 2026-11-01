# Role a oprávnění

## Vestavěné role

| Role | Konstanta | V knihovně |
|---|---|---|
| `Administrator` | `Roles.Administrator` | Smí spravovat uživatele a role |
| `Editor` | `Roles.Editor` | V knihovně nic navíc, je určená pro stránky aplikace |

Další role vytvoří administrátor na stránce `/Identity/Account/RolesUsers/SeznamEditRole`.

## Ochrana vlastních stránek

Knihovna přidává politiky `AdminOnly` (Administrator) a `EditorOrAdmin` (Editor nebo Administrator).

```csharp
[Authorize(Policy = AuthorizationPolicies.EditorOrAdmin)]
public class MojeStrankaModel : PageModel { }
```

Nebo pro celou složku:

```csharp
builder.Services.AddRazorPages(o => o.Conventions.AuthorizeFolder("/Admin", AuthorizationPolicies.AdminOnly));
```

## První administrátor

Nastavte před prvním spuštěním `IdentityBootstrap:AdminEmail` a `IdentityBootstrap:AdminPassword` a po založení je odstraňte. Bez nich nevznikne žádný účet. Veřejná registrace administrátora nevytvoří.

## Pravidla správy

- Posledního administrátora nelze odebrat z role, správa by zůstala bez přístupu.
- Role `Administrator` a `Editor` nelze smazat.
- Smazáním jiné role ji ztratí všichni její uživatelé.
- Účet s libovolnou rolí se bere jako potvrzený i bez potvrzeného e-mailu. Přidělení role tak nahrazuje potvrzení.
- U přihlášeného uživatele se změna role spolehlivě projeví až po novém přihlášení.

Identifikátory v adresách stránek rolí jsou jen zakódované (`CodeDecode`), nejsou tajné. O přístupu rozhoduje politika `AdminOnly`.
