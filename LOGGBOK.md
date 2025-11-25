# Loggbok – Projekt Väderdata

### 📅 21 november – Projektstart och Arkitektur
Jag startade projektet och satte upp strukturen i Visual Studio. För att följa "Best Practices" och kraven delade jag upp lösningen i tre lager:
* **Core:** För modeller och logik.
* **DataAccess:** För databashantering.
* **UI:** För konsolapplikationen.

Jag skapade datamodellen `WeatherData` och installerade Entity Framework Core. Jag konfigurerade `DbContext` för att koppla mot SQL Server LocalDB. Målet för dagen var att få Code First att fungera så att databasen kunde skapas automatiskt.

### 📅 22 november – Dataimport och Felsökning
Fokus idag var att läsa in den autentiska datafilen (`TempFuktData.csv`) till databasen. Detta visade sig vara klurigare än väntat:
1.  **Problem:** Programmet kraschade direkt eftersom CSV-filen saknade kolumnen `Id` (som min databasmodell krävde).
    * **Lösning:** Jag konfigurerade `CsvHelper` med en `ClassMap` för att ignorera ID-kolumnen vid inläsning.
2.  **Problem:** Filen innehöll "smutsig data". Vissa temperaturer (t.ex. -1) var skrivna med felaktiga tecken som inte gick att tolka som siffror.
    * **Lösning:** Jag lade till felhantering (`ReadingExceptionOccurred`) som loggar felet i konsolen och hoppar över den specifika raden istället för att krascha programmet. Resultatet blev en lyckad import av ca 156 000 rader.

### 📅 23 november – Grundläggande Analys (G-krav)
När databasen var fylld började jag bygga logiken i klassen `WeatherProcessor` (i Core-projektet). Jag använde LINQ för att svara på G-kraven:
* Beräkna medeltemperatur och luftfuktighet per dag.
* Sortera fram varmaste/kallaste och torraste/fuktigaste dagarna.
* Beräkna mögelrisk baserat på en algoritm där riskindexet är 0 om det är för kallt eller för torrt.

Jag implementerade även logiken för **Meteorologiska Årstider**. Utmaningen var att hitta sekvenser av 5 dagar i rad som uppfyllde temperaturkraven för höst (<10°C) och vinter (≤0°C).

### 📅 24 november – Avancerad Analys (VG) och Slutförande
Sista dagen ägnades åt de extra kraven för VG och slutpolering.
* **Balkongdörren:** Jag skapade en algoritm som jämför rader tidsmässigt. Om innertemperaturen sjunker *samtidigt* som utetemperaturen stiger, räknas det som att dörren är öppen.
* **Kritiskt fel upptäckt:** Programmet kraschade/fastnade vid beräkningen av balkongdörren. Det visade sig bero på dubbletter i CSV-filen (samma klockslag fanns två gånger).
    * **Lösning:** Jag använde `.GroupBy(...).ToDictionary(...)` för att filtrera bort dubbletterna innan beräkningen.
* **Temperaturdifferens:** Jag implementerade sortering baserat på skillnaden mellan inne- och utetemperatur.

Slutligen städade jag koden, lade till beskrivande kommentarer och skrev en `README.md` med instruktioner för hur man kör programmet.