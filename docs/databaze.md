# Databáze

## Tabulky

Používá se standardní schéma ASP.NET Core Identity 8 bez vlastních sloupců.

| Tabulka | Obsah |
|---|---|
| `AspNetUsers` | Účty včetně otisku hesla, razítka zabezpečení a stavu blokace |
| `AspNetRoles` | Role |
| `AspNetUserRoles` | Přiřazení rolí |
| `AspNetUserClaims`, `AspNetRoleClaims` | Doplňková oprávnění |
| `AspNetUserLogins` | Externí přihlášení |
| `AspNetUserTokens` | Tokeny, například klíč pro dvoufázové ověření |

## Vytvoření

Skript [sql/identity-schema-sqlserver.sql](../sql/identity-schema-sqlserver.sql) je ověřený na SQL Server Express. Začíná nastavením `QUOTED_IDENTIFIER ON`, které potřebují filtrované indexy.

```powershell
sqlcmd -S SERVER -d DATABAZE -E -C -b -i sql/identity-schema-sqlserver.sql
```

Knihovna neobsahuje migrace a při startu schéma nemění. Aplikace, která migrace používá, si je může vytvořit nad `ApplicationDbContext` vlastními nástroji.

## Jiná databáze

```csharp
builder.Services.AddIdentityKit(builder.Configuration, db => db.UseSqlite(connectionString));
```

Tabulky se pak vytvoří například přes `ApplicationDbContext.Database.EnsureCreatedAsync()`, jak to dělá ukázka. Ověřený je SQL Server a SQLite. Balíček pro SQL Server se do projektu dostane vždy.

## Napojení na existující databázi

Existující databáze se schématem ASP.NET Core Identity 8 nepotřebuje žádnou změnu a skript se v ní nespouští. Přesto platí:

1. **Zápisy do účtů.** Knihovna zapisuje do `AspNetUsers` při přihlášení: počítá špatné pokusy a blokace, a pokud má účet starší formát hesla, přepíše jeho otisk. Účet databáze jen pro čtení proto nestačí.
2. **Přihlášení v jiných aplikacích.** Změna hesla nebo razítka `SecurityStamp` časem odhlásí uživatele ze všech aplikací nad stejnou databází. Výchozí kontrola razítka proběhne do 30 minut.
3. **Role platí všude.** Role jsou společné. Pokud aplikace znamenají různá oprávnění, používejte různé názvy rolí.
4. **Vestavěné role.** `InitializeIdentityKitAsync` chybějící role `Administrator` a `Editor` doplní. V cizí databázi tedy zapisuje, stejně jako při nastavení `IdentityBootstrap`.
5. **Chybějící indexy.** Starší databáze nemusí mít všechny indexy ze skriptu. Přihlášení bez nich funguje, chybějící unikátní index ale nezabrání zdvojení uživatelského jména na úrovni databáze.
6. **Přihlášení mezi aplikacemi.** Sdílená databáze sama nezajistí společné přihlášení. Každá aplikace má vlastní cookie a klíče Data Protection.

Pokud je databáze sdílená s provozovaným webem, je bezpečnější, aby se ostatní aplikace přihlašovaly přes tento web, například pomocí OpenID Connect, a do jeho tabulek nesahaly. Knihovna tuto variantu zatím neobsahuje.
