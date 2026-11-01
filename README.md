# Identity: hotová správa účtů a rolí pro ASP.NET Core 8

Knihovna přidá do webové aplikace registraci, přihlášení, obnovu hesla, dvoufázové ověření, správu vlastního účtu a administraci uživatelů a rolí. Rozhraní je převážně česky a běží nad standardním ASP.NET Core Identity.

Vznikla z přihlašování webu Kultura Krupka a je z něj vyčleněná tak, aby ji šlo použít v jiném projektu.

## Co v ní je

| Oblast | Obsah |
|---|---|
| Účty | Registrace s potvrzením e-mailu, přihlášení, odhlášení, zapomenuté heslo, dočasné zablokování po špatných pokusech |
| Vlastní účet | Profil, změna e-mailu a hesla, dvoufázové ověření, stažení a smazání osobních údajů |
| Administrace | Seznam uživatelů, přiřazování rolí, vytváření a mazání rolí |
| Oprávnění | Role `Administrator` a `Editor`, politiky `AdminOnly` a `EditorOrAdmin` |
| E-maily | Odesílání přes SMTP (MailKit), lze nahradit vlastní službou |
| Databáze | SQL Server ve výchozím nastavení, jiný poskytovatel EF Core přes jeden parametr, připravený skript tabulek |

## Rychlé vyzkoušení

Potřebujete jen .NET SDK 8. Ukázka běží nad SQLite a e-maily vypisuje do konzole.

```powershell
git clone https://github.com/zukovVeliky/Identity.git
cd Identity
dotnet run --project samples/IdentitySample
```

Otevřete <http://localhost:5000>. Po registraci najdete potvrzovací odkaz v konzoli. Aby se šlo přihlásit jako administrátor, zadejte účet před spuštěním:

```powershell
$env:IdentityBootstrap__AdminEmail = "admin@example.test"
$env:IdentityBootstrap__AdminPassword = "Vlastni-Silne-Heslo-1"
```

## Použití ve vlastní aplikaci

**1. Přidejte knihovnu do řešení.** Nejjednodušší je podmodul Gitu:

```powershell
git submodule add https://github.com/zukovVeliky/Identity.git external/Identity
dotnet add reference external/Identity/src/ZukovVeliky.Identity
```

**2. Zapojte ji v `Program.cs`:**

```csharp
using ZukovVeliky.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddIdentityKit(builder.Configuration);

var app = builder.Build();
await app.Services.InitializeIdentityKitAsync();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
```

**3. Vytvořte tabulky** spuštěním [sql/identity-schema-sqlserver.sql](sql/identity-schema-sqlserver.sql) a nastavte připojení, odesílání e-mailů a název webu. Hesla a připojovací řetězce patří do proměnných prostředí nebo do souboru mimo Git:

```json
{
  "ConnectionStrings": { "DatabaseIdentity": "..." },
  "IdentityKit": { "SiteName": "Můj web" },
  "EmailSettings": { "MailServer": "smtp.example.com", "Sender": "info@example.com", "SenderName": "Můj web", "User": "...", "Password": "..." }
}
```

Pak jsou k dispozici adresy `/Login`, `/Identity/Account/Register` a další v [přehledu stránek](docs/architektura.md#stranky).

## Dokumentace

| Dokument | Obsah |
|---|---|
| [Instalace](docs/instalace.md) | Postup krok za krokem, vzhled, ověření, nasazení |
| [Konfigurace](docs/konfigurace.md) | Všechna nastavení a jejich výchozí hodnoty |
| [Architektura](docs/architektura.md) | Struktura kódu, přehled stránek, závislosti, testy |
| [Databáze](docs/databaze.md) | Tabulky, jiný poskytovatel, napojení na existující databázi |
| [Role a oprávnění](docs/role-a-opravneni.md) | Vestavěné role, ochrana stránek, první administrátor |
| [Bezpečnost](docs/bezpecnost.md) | Co je řešené, co musí zajistit aplikace, známá omezení |
| [Původ a vlastnosti](docs/puvod-a-zmeny.md) | Z čeho knihovna vznikla a jak je upravená |

## Vývoj

```powershell
dotnet test Identity.sln
```

Testy procházejí skutečné stránky včetně registrace, potvrzení e-mailu, rolí a blokování účtu.

## Licence

[MIT](LICENSE). Bootstrap, jQuery a jQuery Validation ve složce `src/ZukovVeliky.Identity/wwwroot/lib` mají vlastní licence uložené u souborů.
