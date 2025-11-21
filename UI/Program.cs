using DataAccess;

namespace UI
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Startar väderapplikationen...");

			// Using-satsen säkerställer att databasanslutningen stängs korrekt när blocket avslutas.
			using (var context = new WeatherContext())
			{
				var importer = new CsvImporter();

				try
				{
					Console.WriteLine("Kontrollerar databas och importerar data vid behov...");

					// Kör importlogiken som validerar filen och fyller databasen om den är tom.
					importer.LoadDataIfEmpty(context);

					Console.WriteLine("Klar!");
				}
				catch (Exception ex)
				{
					Console.WriteLine("Ett fel uppstod vid inläsning: " + ex.Message);
					return;
				}

				var antalMätningar = context.WeatherData.Count();
				Console.WriteLine($"Databasen innehåller nu {antalMätningar} mätvärden.");

				if (antalMätningar > 0)
				{
					var första = context.WeatherData.First();
					Console.WriteLine($"Exempeldata: {första.Datum} - {första.Plats} - {första.Temp} grader");
				}
			}

			Console.WriteLine("Tryck på valfri tangent för att avsluta.");
			Console.ReadKey();
		}
	}
}