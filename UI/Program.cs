using DataAccess;
using Core;

namespace UI
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Startar väderapplikationen...");

			// Etablerar kontakt med databasen via Entity Framework.
			// Using-blocket säkerställer att resurser frigörs korrekt när programmet stänger.
			using (var context = new WeatherContext())
			{
				// --- STEG 1: IMPORT ---
				// Initierar importprocessen som läser CSV-filen och fyller databasen
				// om den är tom. Hanterar även skapandet av databasen vid första körning.
				var importer = new CsvImporter();
				try
				{
					Console.WriteLine("Kontrollerar databasen...");
					importer.LoadDataIfEmpty(context);
				}
				catch (Exception ex)
				{
					// Vid kritiska fel (t.ex. fil saknas) avbryts körningen kontrollerat.
					Console.WriteLine("Kunde inte importera data: " + ex.Message);
					return;
				}

				// --- STEG 2: ANALYS ---
				// Hämtar all data till minnet för att optimera prestandan vid beräkningarna.
				Console.WriteLine("Hämtar data till minnet för analys...");
				var allData = context.WeatherData.ToList();
				var processor = new WeatherProcessor();

				// 1. Temperatur
				Console.WriteLine("\n--- 1. Temperatur (Ute) ---");

				// Sorterar dagar från varmast till kallast baserat på dygnsmedelvärde.
				var tempSort = processor.SortDaysByTemperature(allData, "Ute");
				if (tempSort.Any())
				{
					Console.WriteLine($"Varmast: {tempSort.First().Date:yyyy-MM-dd} ({tempSort.First().AvgTemp:F1}°C)");
					Console.WriteLine($"Kallast: {tempSort.Last().Date:yyyy-MM-dd} ({tempSort.Last().AvgTemp:F1}°C)");
				}

				// 2. Luftfuktighet
				Console.WriteLine("\n--- 2. Luftfuktighet (Ute) ---");

				// Sorterar dagar från torrast till fuktigast baserat på dygnsmedelvärde.
				var humSort = processor.SortDaysByHumidity(allData, "Ute");
				if (humSort.Any())
				{
					Console.WriteLine($"Torrast: {humSort.First().Date:yyyy-MM-dd} ({humSort.First().AvgHumidity:F1}%)");
					Console.WriteLine($"Fuktigast: {humSort.Last().Date:yyyy-MM-dd} ({humSort.Last().AvgHumidity:F1}%)");
				}

				// 3. Mögelrisk
				Console.WriteLine("\n--- 3. Mögelrisk (Ute) ---");

				// Beräknar riskindex för varje dag och sorterar fram de dagar med högst risk.
				var moldSort = processor.SortDaysByMoldRisk(allData, "Ute");
				if (moldSort.Any())
				{
					var topRisk = moldSort.Last();
					Console.WriteLine($"Högst risk: {topRisk.Date:yyyy-MM-dd} (Index: {topRisk.MoldRisk:F2})");

					Console.WriteLine("Topp 3 riskdagar:");
					foreach (var d in moldSort.AsEnumerable().Reverse().Take(3))
						Console.WriteLine($"  {d.Date:yyyy-MM-dd}: {d.MoldRisk:F2}");
				}

				// 4. Årstider
				Console.WriteLine("\n--- 4. Årstider ---");

				// Letar efter specifika temperatursekvenser (5 dagar i rad) för att fastställa
				// när meteorologisk höst och vinter startade.
				var host = processor.FindMeteorologicalAutumn(allData);
				var vinter = processor.FindMeteorologicalWinter(allData);
				Console.WriteLine($"Höststart: {(host.HasValue ? host.Value.ToString("yyyy-MM-dd") : "Ej funnen")}");
				Console.WriteLine($"Vinterstart: {(vinter.HasValue ? vinter.Value.ToString("yyyy-MM-dd") : "Ej funnen")}");

				// 5. Balkongdörren (VG)
				Console.WriteLine("\n--- 5. Balkongdörren (VG) ---");

				// Analyserar temperaturförändringar (Inne sjunker + Ute stiger) för att uppskatta
				// hur länge dörren stått öppen per dag.
				var balconyDays = processor.CalculateBalconyOpenTime(allData);
				if (balconyDays.Any())
				{
					var mestOpen = balconyDays.First();
					Console.WriteLine($"Dagen dörren var öppen längst: {mestOpen.Date:yyyy-MM-dd}");
					Console.WriteLine($"Total tid den dagen: {mestOpen.TotalMinutes} minuter (baserat på temp-förändringar)");

					Console.WriteLine("Topp 3 dagar:");
					foreach (var d in balconyDays.Take(3))
						Console.WriteLine($"  {d.Date:yyyy-MM-dd}: {d.TotalMinutes} min");
				}
				else
				{
					Console.WriteLine("Kunde inte detektera att balkongdörren varit öppen.");
				}

				// 6. Temp-differens (VG)
				Console.WriteLine("\n--- 6. Temperaturdifferens Inne vs Ute (VG) ---");

				// Jämför skillnaden mellan inne- och utetemperatur för att hitta extremvärden.
				var diffSort = processor.SortDaysByTempDifference(allData);
				if (diffSort.Any())
				{
					var storstDiff = diffSort.First();
					var minstDiff = diffSort.Last();

					Console.WriteLine($"Störst skillnad: {storstDiff.Date:yyyy-MM-dd} (Diff: {storstDiff.AvgDiff:F1}°C)");
					Console.WriteLine($"Minst skillnad: {minstDiff.Date:yyyy-MM-dd} (Diff: {minstDiff.AvgDiff:F1}°C)");
				}
			}

			Console.WriteLine("\nKlar! Tryck tangent för att avsluta.");
			Console.ReadKey();
		}
	}
}