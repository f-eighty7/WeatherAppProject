using System;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
	public class WeatherProcessor
	{
		// --- TEMPERATUR ---

		public double? AverageTemperatureForDate(List<WeatherData> weatherData, DateTime date, string plats)
		{
			// Filtrerar ut mätvärden för angiven plats och datum.
			var selectedData = weatherData
				.Where(w => w.Plats == plats && w.Datum.Date == date.Date)
				.ToList();

			// Returnerar null om ingen data hittas för att undvika felaktiga nollvärden.
			if (!selectedData.Any()) return null;

			// Beräknar och returnerar medelvärdet.
			return selectedData.Average(w => w.Temp);
		}

		public List<(DateTime Date, double AvgTemp)> SortDaysByTemperature(List<WeatherData> weatherData, string plats)
		{
			return weatherData
				.Where(w => w.Plats == plats)
				.GroupBy(w => w.Datum.Date) // Grupperar alla mätvärden per dag.
				.Select(g => new
				{
					Date = g.Key,
					AvgTemp = g.Average(w => w.Temp) // Beräknar dygnsmedeltemperaturen.
				})
				.OrderByDescending(x => x.AvgTemp) // Sorterar från varmast till kallast.
				.Select(x => (x.Date, x.AvgTemp))
				.ToList();
		}

		// --- LUFTFUKTIGHET ---

		public List<(DateTime Date, double AvgHumidity)> SortDaysByHumidity(List<WeatherData> weatherData, string plats)
		{
			return weatherData
				.Where(w => w.Plats == plats)
				.GroupBy(w => w.Datum.Date)
				.Select(g => new
				{
					Date = g.Key,
					AvgHumidity = g.Average(w => w.Luftfuktighet) // Beräknar dygnsmedelluftfuktighet.
				})
				.OrderBy(x => x.AvgHumidity) // Sorterar från lägst (torrast) till högst fuktighet.
				.Select(x => (x.Date, x.AvgHumidity))
				.ToList();
		}

		// --- MÖGELRISK ---

		public List<(DateTime Date, double MoldRisk)> SortDaysByMoldRisk(List<WeatherData> weatherData, string plats)
		{
			return weatherData
				.Where(w => w.Plats == plats)
				.GroupBy(w => w.Datum.Date)
				.Select(g => new
				{
					Date = g.Key,
					// Hämtar dygnsmedelvärden för både temperatur och fuktighet.
					AvgTemp = g.Average(w => w.Temp),
					AvgHumidity = g.Average(w => w.Luftfuktighet)
				})
				.Select(x => new
				{
					x.Date,
					// Använder hjälpmetoden för att beräkna riskindex för varje dag.
					Risk = CalculateMoldIndex(x.AvgTemp, x.AvgHumidity)
				})
				.OrderBy(x => x.Risk) // Sorterar från lägst risk (0) till högst.
				.Select(x => (x.Date, x.Risk))
				.ToList();
		}

		// Hjälpmetod som applicerar en förenklad formel för mögelrisk.
		private double CalculateMoldIndex(double temp, double humidity)
		{
			// Mögel växer inte effektivt vid minusgrader eller låg luftfuktighet.
			if (temp < 0 || humidity < 78) return 0;

			// Beräknar ett index där högre värme och fukt ger högre risk.
			double risk = ((humidity - 78) * (temp / 15));
			return risk < 0 ? 0 : risk;
		}

		// --- METEOROLOGISKA ÅRSTIDER ---

		public DateTime? FindMeteorologicalAutumn(List<WeatherData> weatherData)
		{
			// Förbereder dygnsmedelvärden sorterade i datumordning.
			var dailyTemps = weatherData
				.Where(w => w.Plats == "Ute")
				.GroupBy(w => w.Datum.Date)
				.Select(g => new { Date = g.Key, AvgTemp = g.Average(w => w.Temp) })
				.OrderBy(x => x.Date)
				.ToList();

			// Letar efter en sekvens på 5 dagar i rad.
			for (int i = 0; i <= dailyTemps.Count - 5; i++)
			{
				bool isAutumn = true;
				// Kontrollerar om alla 5 dagar i sekvensen är under 10 grader.
				for (int j = 0; j < 5; j++)
				{
					if (dailyTemps[i + j].AvgTemp >= 10.0)
					{
						isAutumn = false;
						break; // Bryter inre loopen om villkoret inte uppfylls.
					}
				}

				// Om villkoret höll för hela sekvensen, returneras startdatumet.
				if (isAutumn) return dailyTemps[i].Date;
			}
			return null;
		}

		public DateTime? FindMeteorologicalWinter(List<WeatherData> weatherData)
		{
			var dailyTemps = weatherData
				.Where(w => w.Plats == "Ute")
				.GroupBy(w => w.Datum.Date)
				.Select(g => new { Date = g.Key, AvgTemp = g.Average(w => w.Temp) })
				.OrderBy(x => x.Date)
				.ToList();

			for (int i = 0; i <= dailyTemps.Count - 5; i++)
			{
				bool isWinter = true;
				// Kontrollerar om alla 5 dagar i sekvensen är 0 grader eller kallare.
				for (int j = 0; j < 5; j++)
				{
					if (dailyTemps[i + j].AvgTemp > 0.0)
					{
						isWinter = false;
						break;
					}
				}
				if (isWinter) return dailyTemps[i].Date;
			}
			return null;
		}

		// --- VG-METODER ---

		public List<(DateTime Date, int TotalMinutes)> CalculateBalconyOpenTime(List<WeatherData> weatherData)
		{
			// Skapar dictionaries för snabbare uppslagning av data per tidpunkt.
			var inneData = weatherData.Where(w => w.Plats == "Inne").ToDictionary(w => w.Datum);
			var uteData = weatherData.Where(w => w.Plats == "Ute").ToDictionary(w => w.Datum);

			var timestamps = inneData.Keys.OrderBy(t => t).ToList();
			var openDoorMinutesPerDay = new Dictionary<DateTime, int>();

			// Loopar igenom tidpunkterna och jämför varje mätning med den föregående.
			for (int i = 1; i < timestamps.Count; i++)
			{
				var currentTime = timestamps[i];
				var prevTime = timestamps[i - 1];

				// Ignorerar luckor i datan som är större än 15 minuter.
				if ((currentTime - prevTime).TotalMinutes > 15) continue;

				if (inneData.ContainsKey(currentTime) && inneData.ContainsKey(prevTime) &&
					uteData.ContainsKey(currentTime) && uteData.ContainsKey(prevTime))
				{
					double inneDelta = inneData[currentTime].Temp - inneData[prevTime].Temp;
					double uteDelta = uteData[currentTime].Temp - uteData[prevTime].Temp;

					// Detekterar "Balkong-mönster": Innertemp sjunker samtidigt som utetemp stiger.
					if (inneDelta < 0 && uteDelta > 0)
					{
						var day = currentTime.Date;
						if (!openDoorMinutesPerDay.ContainsKey(day))
							openDoorMinutesPerDay[day] = 0;

						// Adderar tidsintervallet till dagens total.
						openDoorMinutesPerDay[day] += (int)(currentTime - prevTime).TotalMinutes;
					}
				}
			}

			return openDoorMinutesPerDay
				.Select(kvp => (kvp.Key, kvp.Value))
				.OrderByDescending(x => x.Value)
				.ToList();
		}

		public List<(DateTime Date, double AvgDiff)> SortDaysByTempDifference(List<WeatherData> weatherData)
		{
			var inneData = weatherData.Where(w => w.Plats == "Inne");
			var uteData = weatherData.Where(w => w.Plats == "Ute");

			// Slår ihop (joinar) inne- och utedata baserat på datum för att kunna jämföra.
			var query = from inne in inneData
						join ute in uteData on inne.Datum equals ute.Datum
						select new
						{
							inne.Datum,
							Diff = Math.Abs(inne.Temp - ute.Temp) // Beräknar absolut skillnad.
						};

			// Grupperar diffarna per dag och tar fram snittet.
			return query
				.GroupBy(x => x.Datum.Date)
				.Select(g => new
				{
					Date = g.Key,
					AvgDiff = g.Average(x => x.Diff)
				})
				.OrderByDescending(x => x.AvgDiff) // Sorterar störst skillnad först.
				.Select(x => (x.Date, x.AvgDiff))
				.ToList();
		}
	}
}