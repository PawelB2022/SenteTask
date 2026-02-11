# DbMetaTool

Konsolowa aplikacja napisana w .NET 8.0.

Celem projektu jest praca na metadanych bazy danych Firebird 5.0:

* tworzenie bazy danych na podstawie skryptów SQL,
* eksport metadanych do plików,
* aktualizacja istniejącej bazy danych przy użyciu skryptów.

Projekt realizowany jako zadanie rekrutacyjne.

---

## Wykonane kroki

### Konfiguracja projektu

1. Utworzono projekt konsolowy w technologii .NET 8.0.

   * Narzędzie: **.NET CLI**

   ```bash
   dotnet new console -n DbMetaTool -f net8.0
   ```

2. Skonfigurowano strukturę projektu, w której:

   * `Program.cs` oraz `DbMetaTool.csproj` znajdują się w katalogu głównym,
   * cała logika aplikacji znajduje się w katalogu `src/`.

3. Dodano zależność do providera ADO.NET dla Firebird.

   * Narzędzie: **.NET CLI**

   ```bash
   dotnet add package FirebirdSql.Data.FirebirdClient
   ```

---

### Konfiguracja Firebird

4. Zainstalowano i skonfigurowano lokalny serwer **Firebird 5.0**.

   * Narzędzie: instalator Firebird
   * Użytkownik administracyjny: `SYSDBA`
   * Hasło testowe: `masterkey`

5. Zweryfikowano poprawność logowania do serwera Firebird.

   * Narzędzie: **isql**

   ```bash
   isql -user SYSDBA -password masterkey
   ```

---

### Ręczne utworzenie testowej bazy danych

6. Utworzono testową bazę danych Firebird.

   * Narzędzie: **isql**

   ```sql
   CREATE DATABASE 'C:\sciezka_projektu\database\manual_test.fdb'
   USER 'SYSDBA' PASSWORD 'masterkey'
   DEFAULT CHARACTER SET UTF8;
   ```

7. Wykonano ręcznie skrypty SQL tworzące strukturę bazy danych za pomocą narzędzia **isql** w katalogu `manual_db/`:

   * domeny,

   * tabele,

   * procedurę składowaną.

   ```sql
   INPUT '01_domains.sql';
   INPUT '02_tables.sql';
   INPUT '03_procedures.sql';
   ```

---

### Weryfikacja aplikacji

8. Zaimplementowano podstawowe komendy CLI aplikacji:

   * `build-db`
   * `export-scripts`
   * `update-db`

   Na obecnym etapie komendy weryfikują poprawność argumentów oraz połączenie z bazą danych.

9. Zweryfikowano działanie aplikacji z poziomu konsoli.

   * Narzędzie: **.NET CLI**

   ```bash
   dotnet build
   dotnet run -- build-db --db-dir "C:\sciezka_projektu\database" --scripts-dir "C:\sciezka_projektu\scripts"
   dotnet run -- export-scripts --connection-string "<connection_string>" --output-dir "C:\sciezka_projektu\out"
   dotnet run -- update-db --connection-string "<connection_string>" --scripts-dir "C:\sciezka_projektu\scripts"
   ```

---

## Nowe właściwości programu

Na obecnym etapie implementacji aplikacja posiada następujące właściwości:

* Budowanie nowej bazy danych Firebird na podstawie zestawu skryptów SQL (`build-db`).
* Deterministyczne wykonywanie skryptów w kolejności alfabetycznej nazw plików.
* Obsługa skryptów zawierających dyrektywę `SET TERM`, umożliwiającą poprawne tworzenie procedur składowanych.
* Wykonywanie skryptów w transakcjach (jedna transakcja na plik), co pozwala na wycofanie zmian w przypadku błędu.
* Generowanie czytelnego raportu z procesu budowy bazy danych, zawierającego:

  * informację o utworzeniu bazy danych,
  * liczbę przetworzonych plików,
  * liczbę wykonanych instrukcji SQL,
  * listę błędów z nazwą pliku i fragmentem problematycznej instrukcji.
* Współdzielenie logiki technicznej (ładowanie skryptów, dzielenie instrukcji SQL, wykonywanie poleceń) pomiędzy komendami `build-db` oraz `update-db`.
* Oddzielenie logiki infrastrukturalnej (Firebird, SQL, IO plików) od logiki sterującej komendami CLI.