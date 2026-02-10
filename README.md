# DbMetaTool

Konsolowa aplikacja napisana w .NET 8.0.

Celem projektu jest praca na metadanych bazy danych Firebird 5.0:
- tworzenie bazy danych na podstawie skryptów SQL,
- eksport metadanych do plików,
- aktualizacja istniejącej bazy danych przy użyciu skryptów.

Projekt realizowany jako zadanie rekrutacyjne.

---

## Technologie

- .NET 8.0
- Firebird 5.0

---

## Struktura projektu

- `Program.cs` – punkt wejścia aplikacji
- `DbMetaTool.csproj` – konfiguracja projektu
- `src/` – cała logika aplikacji
- `scripts/` – przykładowe skrypty SQL

---

## Uruchamianie

Projekt można zbudować i uruchomić za pomocą .NET CLI:

```bash
dotnet build
dotnet run
