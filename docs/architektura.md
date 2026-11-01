# Architektura

## Struktura

```
src/ZukovVeliky.Identity/          Knihovna (Razor Class Library)
  IdentityKit.cs                   AddIdentityKit, InitializeIdentityKitAsync, nastavení, politiky
  IdentityCore.cs                  DbContext, role, potvrzování účtů, založení účtů, e-maily
  Areas/Identity/Pages/            Stránky účtů a administrace
  Pages/Shared/_Layout.cshtml      Náhradní rozložení
  wwwroot/lib/                     Bootstrap a jQuery včetně licencí
samples/IdentitySample/            Ukázková aplikace
tests/ZukovVeliky.Identity.Tests/  Testy skutečných stránek
sql/                               Skript tabulek pro SQL Server
```

Knihovna staví na `AddDefaultIdentity<IdentityUser>` a vlastní stránky mají přednost před stránkami z balíčku `Microsoft.AspNetCore.Identity.UI`. Jeden test ověřuje, že se opravdu zobrazují ty české.

## Stránky

Předpona adres je `/Identity/Account`.

| Adresa | Účel | Přístup |
|---|---|---|
| `/Login` | Přihlášení, navíc krátká adresa `/Login` bez předpony | Veřejný |
| `/Logout` | Odhlášení | Přihlášený |
| `/Register`, `/RegisterConfirmation` | Registrace | Veřejný |
| `/ConfirmEmail`, `/ResendEmailConfirmation` | Potvrzení e-mailu | Veřejný |
| `/ForgotPassword`, `/ResetPassword` | Obnova hesla | Veřejný |
| `/LoginWith2fa`, `/LoginWithRecoveryCode` | Dvoufázové přihlášení | Rozpracované přihlášení |
| `/ExternalLogin` | Externí přihlášení | Veřejný |
| `/AccessDenied`, `/Lockout` | Informační stránky | Veřejný |
| `/Manage/Index`, `/Manage/Email`, `/Manage/ChangePassword`, `/Manage/SetPassword` | Vlastní účet | Přihlášený |
| `/Manage/TwoFactorAuthentication`, `/Manage/EnableAuthenticator`, `/Manage/Disable2fa`, `/Manage/ResetAuthenticator`, `/Manage/GenerateRecoveryCodes`, `/Manage/ShowRecoveryCodes` | Dvoufázové ověření | Přihlášený |
| `/Manage/PersonalData`, `/Manage/DownloadPersonalData`, `/Manage/DeletePersonalData` | Osobní údaje | Přihlášený |
| `/Manage/ExternalLogins` | Externí účty | Přihlášený |
| `/RolesUsers/SeznamUsers`, `/RolesUsers/UserEditRole` | Uživatelé a jejich role | `AdminOnly` |
| `/RolesUsers/SeznamEditRole` | Správa rolí | `AdminOnly` |

Texty jsou převážně česky, některé stránky vlastního účtu zůstaly anglicky jako ve výchozí šabloně.

## Závislosti

| Balíček | Verze | K čemu slouží |
|---|---|---|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 8.0.16 | Uložení účtů v databázi |
| `Microsoft.AspNetCore.Identity.UI` | 8.0.16 | Základ přihlašování a rozhraní `IEmailSender` |
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.16 | Výchozí databáze |
| `MailKit`, `MimeKit` | 4.18.2 | Odesílání e-mailů |
| Bootstrap 5.1.0, jQuery 3.6.0, jQuery Validation | v `wwwroot/lib` | Vzhled a kontrola formulářů. Licence jsou u souborů. |

Ukázka navíc používá `Microsoft.EntityFrameworkCore.Sqlite`, testy xUnit a `Microsoft.AspNetCore.Mvc.Testing`.

## Testy

```powershell
dotnet test Identity.sln
```

Testy spouštějí ukázkovou aplikaci v paměti nad dočasnou databází SQLite a procházejí stránky jako prohlížeč.

| Oblast | Co se ověřuje |
|---|---|
| Rozhraní | Zobrazují se české stránky, funguje `/Login` |
| Registrace | Přihlášení až po potvrzení e-mailu, opakované poslání odkazu, vypnutí registrace |
| Obnova hesla | Celý průchod od žádosti po přihlášení novým heslem |
| Založení účtů | Bez nastavení žádný účet nevznikne, opakované spuštění nic nezdvojí |
| Oprávnění | Stránky správy pro nepřihlášeného, editora a administrátora |
| Role | Přidání a odebrání, role jako náhrada potvrzení, ochrana posledního administrátora a vestavěných rolí |
| Bezpečnost | Zablokování po špatných heslech, žádné výchozí heslo |
| Databáze | Skript v `sql/` odpovídá modelu |

Po změně modelu se skript obnoví spuštěním testů s proměnnou `UPDATE_SCHEMA_SCRIPT=1`.
