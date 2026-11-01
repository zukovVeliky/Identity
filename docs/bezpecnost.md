# Bezpečnost

## Co je řešené

| Oblast | Řešení |
|---|---|
| Hesla | Výchozí hasher ASP.NET Core Identity 8, tedy PBKDF2. Heslo se nikde neukládá v čitelné podobě. |
| Hádání hesla | Účet se po 5 špatných pokusech zablokuje na 5 minut |
| Nové účty | Přihlášení až po potvrzení e-mailu nebo přidělení role |
| Formuláře | Ochrana proti CSRF standardními tokeny Razor Pages |
| Administrace | Stránky rolí vyžadují politiku `AdminOnly` |
| Výchozí účty | Žádné. Bez nastavení nevznikne účet a v kódu není žádné heslo. |
| Dvoufázové ověření | K dispozici pro každého uživatele, není vynucené |

## Co musí zajistit aplikace

- **HTTPS** v provozu, včetně `UseHttpsRedirection` a `UseHsts`.
- **Tajné údaje mimo Git:** připojovací řetězce, hesla SMTP a `IdentityBootstrap`. Repozitář je veřejný a `.gitignore` chrání jen obvyklé názvy souborů.
- **Záloha a ochrana databáze.**
- **Omezení počtu požadavků** na přihlášení a registraci. Blokace chrání jeden účet, ne celý web před hromadnými pokusy nebo zakládáním účtů.
- **Vlastní hlavičky zabezpečení**, například Content Security Policy.

## Data Protection

Přihlašovací cookie a odkazy z e-mailů jsou chráněné klíči Data Protection. Pokud se klíče při restartu nebo nasazení ztratí, všichni se odhlásí a odkazy přestanou platit. Na hostingu je proto ukládejte do trvalého umístění mimo veřejnou složku:

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(cestaKeKlicum))
    .SetApplicationName("MojeAplikace");
```

Klíče nikdy nedávejte do Gitu ani nenahrávejte s aplikací z vývojového počítače.

## Známá omezení

- **Žádná kontrola uniklých hesel** ani vynucené dvoufázové ověření pro administrátory.
- **Bez auditu:** změny rolí se nikam nezapisují, kromě běžných logů aplikace.
- **Registrace prozrazuje obsazený e-mail.** Stránka hlásí, že uživatelské jméno už existuje, jako výchozí šablona Identity.
- **Knihovna neomezuje počet e-mailů**, které lze vyvolat opakovaným posláním odkazu.
- **Dvoufázové ověření a externí přihlášení** nejsou pokryté automatickými testy.
- **Citlivé odkazy v logu:** ukázkový `ConsoleEmailSender` vypisuje potvrzovací odkazy do konzole a nepatří do provozu.

## Hlášení chyb

Bezpečnostní problém prosím nehlaste veřejně v Issues, ale přes [soukromé hlášení zranitelnosti](https://github.com/zukovVeliky/Identity/security/advisories/new) na GitHubu, pokud je zapnuté, případně přímo správci repozitáře.
