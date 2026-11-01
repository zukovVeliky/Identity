# Konfigurace

Nastavení se čte z běžné konfigurace ASP.NET Core. V proměnných prostředí se místo dvojtečky píšou dvě podtržítka, například `EmailSettings__Password`.

## ConnectionStrings

| Klíč | Popis |
|---|---|
| `DatabaseIdentity` | Připojení k databázi. Povinné, pokud `AddIdentityKit` nedostane vlastní nastavení databáze. |

## IdentityKit

| Klíč | Výchozí | Popis |
|---|---|---|
| `SiteName` | `Web` | Název v e-mailech a v aplikacích pro dvoufázové ověření |
| `AllowPublicRegistration` | `true` | Při `false` vrací registrace 404 a účty zakládá jen správce mimo tuto stránku |
| `LoginRoute` | `/Login` | Krátká adresa přihlášení. Prázdná hodnota ji vypne, `/Identity/Account/Login` funguje vždy. |

## IdentityBootstrap

Účty vytvořené při `InitializeIdentityKitAsync`. **Žádné výchozí hodnoty nejsou**, bez nastavení vzniknou jen role.

| Klíč | Popis |
|---|---|
| `AdminEmail`, `AdminPassword` | Účet s rolí `Administrator` |
| `EditorEmail`, `EditorPassword` | Účet s rolí `Editor` |

Účet se založí, jen když ještě neexistuje. Heslo existujícího účtu se nemění, pouze se doplní role. Po založení prvního administrátora doporučujeme hesla z konfigurace odstranit.

## EmailSettings

| Klíč | Výchozí | Popis |
|---|---|---|
| `MailServer` | prázdné | SMTP server |
| `MailPort` | `465` | Port |
| `UseSsl` | `true` | `true` znamená šifrování od začátku spojení, `false` přechod na TLS, pokud ho server nabízí |
| `Sender`, `SenderName` | prázdné | Adresa a jméno odesílatele |
| `User`, `Password` | prázdné | Přihlášení k serveru. Bez `User` se přihlášení vynechá. |

Bez funkčního serveru účet při registraci vznikne, ale e-mail se neodešle. Stránka to oznámí a odkaz lze poslat znovu.

## Pravidla Identity

`AddIdentityKit` vrací `IdentityBuilder` a další nastavení se doplňuje standardně:

```csharp
builder.Services.AddIdentityKit(builder.Configuration);
builder.Services.Configure<IdentityOptions>(o =>
{
    o.Password.RequiredLength = 12;
    o.Lockout.MaxFailedAccessAttempts = 3;
});
builder.Services.ConfigureApplicationCookie(o => o.ExpireTimeSpan = TimeSpan.FromDays(7));
```

Výchozí pravidla jsou rámcová: heslo aspoň 6 znaků s velkým a malým písmenem, číslicí a zvláštním znakem, blokace na 5 minut po 5 špatných pokusech.

Externí přihlášení, například Google, se přidá přes `AddAuthentication().AddGoogle(...)`. Stránky pro něj v knihovně jsou, ale nebylo ověřované.
