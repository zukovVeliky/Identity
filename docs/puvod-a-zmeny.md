# Původ a vlastnosti knihovny

Knihovna vychází z přihlašování webu Kultura Krupka, které je postavené na šabloně ASP.NET Core Identity. Pro samostatné použití je upravená takto:

| Oblast | Řešení v knihovně |
|---|---|
| Výchozí účty | Žádné, účty vznikají jen z nastavení `IdentityBootstrap` |
| Správa rolí | Přes `UserManager` a `RoleManager`, s jedním `DbContext` |
| Ochrana správy | Posledního administrátora nelze odebrat a vestavěné role nelze smazat |
| Blokace účtu | Špatné pokusy o přihlášení se počítají |
| Název webu | Nastavitelný v `IdentityKit:SiteName`, platí pro e-maily i aplikace pro dvoufázové ověření |
| Klientské knihovny | Bootstrap a jQuery jsou součástí knihovny |

Části, které patřily jen konkrétnímu webu, jsou odstraněné. Schéma tabulek zůstává standardní, takže knihovna může pracovat s existující databází ASP.NET Core Identity 8. Podmínky popisuje [Databáze](databaze.md#napojení-na-existující-databázi).
