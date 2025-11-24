using DataAccess;
using Core;

namespace UI
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Startar väderapplikationen...");

			// Skapar en koppling till databasen. Using-satsen garanterar att kopplingen 
			// stängs korrekt när arbetet är klart för att frigöra resurser.
			using (var context = new WeatherContext())
			{
				// --- STEG 1: IMPORT ---
				// Initierar importprocessen. Logiken inuti LoadDataIfEmpty kontrollerar själv
				// om databasen saknar data och behöver fyllas från CSV-filen.
				var importer = new CsvImporter();
				try
				{
					Console.WriteLine("Kontrollerar databasen...");
					importer.LoadDataIfEmpty(context);
				}
				catch (Exception ex)
				{
					// Fångar kritiska fel vid import (t.ex. om filen saknas) och avslutar
					// programmet kontrollerat eftersom analysen inte kan göras utan data.
					Console.WriteLine("Kunde inte importera data: " + ex.Message);
					return;
				}

				// --- STEG 2: ANALYS ---
				// Hämtar all väderdata från databasen till minnet (RAM) i en enda sökning.
				// Detta görs för att effektivisera beräkningarna och undvika tusentals 
				// långsamma anrop mot databasen under analysfasen.
				Console.WriteLine("Hämtar data till minnet för analys...");
				var allData = context.WeatherData.ToList();

				// Instansierar logik-klassen som hanterar alla beräkningar och sorteringar.
				var processor = new WeatherProcessor();

				// 1. Temperatur
				Console.WriteLine("\n--- Temperaturanalys (Ute) ---");

				// Använder processorn för att sortera dagar från varmast till kallast.
				// Hämtar sedan ut första och sista elementet för att visa extremerna.
				var tempSort = processor.SortDaysByTemperature(allData, "Ute");
				if (tempSort.Count > 0)
				{
					var varmaste = tempSort.First();
					var kallaste = tempSort.Last();
					Console.WriteLine($"Varmaste dagen: {varmaste.Date.ToShortDateString()} ({varmaste.AvgTemp:F1} grader)");
					Console.WriteLine($"Kallaste dagen: {kallaste.Date.ToShortDateString()} ({kallaste.AvgTemp:F1} grader)");
				}

				// 2. Luftfuktighet
				Console.WriteLine("\n--- Luftfuktighetsanalys (Ute) ---");

				// Sorterar dagar baserat på genomsnittlig luftfuktighet (lägst till högst).
				var humSort = processor.SortDaysByHumidity(allData, "Ute");
				if (humSort.Count > 0)
				{
					var torraste = humSort.First();
					var fuktigaste = humSort.Last();
					Console.WriteLine($"Torraste dagen: {torraste.Date.ToShortDateString()} ({torraste.AvgHumidity:F1}%)");
					Console.WriteLine($"Fuktigaste dagen: {fuktigaste.Date.ToShortDateString()} ({fuktigaste.AvgHumidity:F1}%)");
				}

				// 3. Mögelrisk
				Console.WriteLine("\n--- Mögelrisk (Ute) ---");

				// Beräknar mögelindex för alla dagar och sorterar från lägst till högst risk.
				var moldSort = processor.SortDaysByMoldRisk(allData, "Ute");
				if (moldSort.Count > 0)
				{
					var hogstRisk = moldSort.Last();
					var nollRisk = moldSort.First();
					Console.WriteLine($"Störst mögelrisk: {hogstRisk.Date.ToShortDateString()} (Riskindex: {hogstRisk.MoldRisk:F2})");

					// Visar en topplista genom att vända på sorteringen och ta de 5 första dagarna
					// med högst riskvärde.
					Console.WriteLine("\nTopp 5 dagar med störst mögelrisk:");
					foreach (var day in moldSort.AsEnumerable().Reverse().Take(5))
					{
						Console.WriteLine($"{day.Date.ToShortDateString()}: Index {day.MoldRisk:F2}");
					}
				}

				// 4. Meteorologiska Årstider
				Console.WriteLine("\n--- Meteorologiska Årstider ---");

				// Letar efter specifika temperatursekvenser (t.ex. 5 dagar i rad under 10 grader)
				// för att fastställa när hösten och vintern anlände meteorologiskt.
				var autumnStart = processor.FindMeteorologicalAutumn(allData);
				if (autumnStart.HasValue)
					Console.WriteLine($"Meteorologisk höst anlände: {autumnStart.Value.ToShortDateString()}");
				else
					Console.WriteLine("Ingen meteorologisk höst hittades i datan.");

				var winterStart = processor.FindMeteorologicalWinter(allData);
				if (winterStart.HasValue)
					Console.WriteLine($"Meteorologisk vinter anlände: {winterStart.Value.ToShortDateString()}");
				else
					Console.WriteLine("Ingen meteorologisk vinter hittades (kanske var den för mild?).");

			}

			Console.WriteLine("\nTryck på valfri tangent för att avsluta.");
			Console.ReadKey();
		}
	}
}