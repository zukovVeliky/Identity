# Instalace

## Požadavky

- .NET SDK 8 a aplikace na `net8.0`
- Aplikace s Razor Pages nebo MVC
- SQL Server, případně jiná databáze s poskytovatelem EF Core

## 1. Připojení knihovny

**Podmodul Gitu** dovoluje knihovnu později aktualizovat a pevně určit verzi:

```powershell
git submodule add https://github.com/zukovVeliky/Identity.git external/Identity
dotnet add reference external/Identity/src/ZukovVeliky.Identity
```

Po naklonování aplikace je potřeba `git submodule update --init`.

**Kopie složky:** stačí zkopírovat `src/ZukovVeliky.Identity` do řešení a přidat na ni odkaz. Aktualizace se pak přenáší ručně.

NuGet balíček se zatím nepublikuje.

## 2. Registrace služeb

```csharp
builder.Services.AddIdentityKit(builder.Configuration);
```

Volání přidá `DbContext`, Identity, politiky oprávnění, Razor Pages a odesílání e-mailů. Jiná databáze se předá druhým parametrem:

```csharp
builder.Services.AddIdentityKit(builder.Configuration, db => db.UseSqlite(connectionString));
```

Vlastní odesílání e-mailů zaregistrujte **před** tímto voláním:

```csharp
builder.Services.AddSingleton<IEmailSender, MujOdesilatel>();
builder.Services.AddIdentityKit(builder.Configuration);
```

## 3. Pořadí v aplikaci

`UseAuthentication` musí být před `UseAuthorization` a obojí po `UseRouting`. `UseStaticFiles` poskytuje Bootstrap a jQuery z knihovny.

```csharp
var app = builder.Build();
await app.Services.InitializeIdentityKitAsync();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
```

`InitializeIdentityKitAsync` vytvoří role `Administrator` a `Editor` a účty z `IdentityBootstrap`. Tabulky už musí existovat a metoda je bezpečné volat opakovaně.

## 4. Tabulky

```powershell
sqlcmd -S SERVER -d DATABAZE -E -C -b -i sql/identity-schema-sqlserver.sql
```

Skript předpokládá prázdnou databázi nebo aspoň databázi bez tabulek `AspNet*`. Existující databázi řeší [Databáze](databaze.md).

## 5. Vzhled

Stránky používají rozložení `/Pages/Shared/_Layout.cshtml`.

- **Aplikace ho má:** použije se automaticky. Musí obsahovat `@await RenderSectionAsync("Scripts", required: false)` a načítat jQuery a Bootstrap, jinak nefunguje kontrola formulářů na straně prohlížeče.
- **Aplikace ho nemá:** použije se jednoduché náhradní rozložení z knihovny.

Odkazy do menu najdete v [ukázkovém rozložení](../samples/IdentitySample/Pages/Shared/_Layout.cshtml). Soubory knihovny jsou dostupné pod `~/_content/ZukovVeliky.Identity/lib/`.

Libovolnou stránku lze nahradit souborem na stejné cestě v aplikaci, například `Areas/Identity/Pages/Account/Login.cshtml`.

## 6. Ověření

1. Otevřete `/Login`, má se ukázat česká přihlašovací stránka.
2. Zaregistrujte účet a potvrďte odkaz z e-mailu.
3. Přihlaste se jako administrátor a otevřete `/Identity/Account/RolesUsers/SeznamUsers`.

## Nasazení

- Statické soubory fungují ve vývojovém prostředí a po `dotnet publish`. Při `dotnet run` v jiném než vývojovém prostředí vrací 404.
- Klíče Data Protection musí přežít restart aplikace, jinak přestanou platit přihlášení i odkazy z e-mailů. Viz [Bezpečnost](bezpecnost.md#data-protection).
- Hesla a připojovací řetězce nepatří do Gitu.
