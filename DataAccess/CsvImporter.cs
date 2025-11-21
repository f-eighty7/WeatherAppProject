using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using Core;

namespace DataAccess
{
	public class CsvImporter
	{
		public void LoadDataIfEmpty(WeatherContext context)
		{
			// Skapar databasen och dess tabeller om de inte redan existerar.
			context.Database.EnsureCreated();

			// Avbryter importen om det redan finns data för att undvika dubbletter.
			if (context.WeatherData.Any())
			{
				return;
			}

			string path = "../../../TempFuktData.csv";

			var config = new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				Delimiter = ",",
				// Konfigurerar felhantering: Vid formatfel (t.ex. ogiltiga tecken) loggas felet 
				// och "return false" instruerar läsaren att hoppa över den raden istället för att krascha.
				ReadingExceptionOccurred = args =>
				{
					Console.WriteLine($"Fel vid inläsning av rad {args.Exception.Context.Parser.Row}: {args.Exception.Message}");
					return false;
				}
			};

			using (var reader = new StreamReader(path))
			using (var csv = new CsvReader(reader, config))
			{
				// Aktiverar den anpassade mappningen för att hantera avsaknaden av ID-kolumn i filen.
				csv.Context.RegisterClassMap<WeatherDataMap>();

				// Läser in filen och filtrerar automatiskt bort rader som orsakade fel i konfigurationen ovan.
				var records = csv.GetRecords<WeatherData>().ToList();

				Console.WriteLine($"Lyckades läsa in {records.Count} giltiga rader.");

				// Sparar alla giltiga poster till databasen i en batch-operation.
				context.WeatherData.AddRange(records);
				context.SaveChanges();
			}
		}
	}

	public class WeatherDataMap : ClassMap<WeatherData>
	{
		public WeatherDataMap()
		{
			AutoMap(CultureInfo.InvariantCulture);
			// Ignorerar ID-egenskapen vid inläsning eftersom den genereras av databasen.
			Map(m => m.Id).Ignore();
		}
	}
}