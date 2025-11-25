namespace Core
{
	public class WeatherProcessor
	{
		// --- G-NIVÅ: TEMPERATUR ---

		// Beräknar medeltemperatur för ett specifikt datum och plats.
		// Returnerar null om ingen data finns för det datumet.
		public double? AverageTemperatureForDate(List<WeatherData> weatherData, DateTime date, string plats)
		{
			var selectedData = weatherData
				.Where(w => w.Plats == plats && w.Datum.Date == date.Date)
				.ToList();

			if (!selectedData.Any()) return null;

			return selectedData.Average(w => w.Temp);
		}

		// Sorterar dagar från varmast till kallast baserat på dygnsmedeltemperatur.
		public List<(DateTime Date, double AvgTemp)> SortDaysByTemperature(List<WeatherData> weatherData, string plats)
		{
			return weatherData
				.Where(w => w.Plats == plats)
				.GroupBy(w => w.Datum.Date) // Grupperar alla mätpunkter per dag
				.Select(g => new
				{
					Date = g.Key,
					AvgTemp = g.Average(w => w.Temp) // Räknar ut snittet för dagen
				})
				.OrderByDescending(x => x.AvgTemp) // Sorterar fallande (högst först)
				.Select(x => (x.Date, x.AvgTemp))
				.ToList();
		}

		// --- G-NIVÅ: LUFTFUKTIGHET ---

		// Sorterar dagar från torrast till fuktigast baserat på dygnsmedelluftfuktighet.
		public List<(DateTime Date, double AvgHumidity)> SortDaysByHumidity(List<WeatherData> weatherData, string plats)
		{
			return weatherData
				.Where(w => w.Plats == plats)
				.GroupBy(w => w.Datum.Date)
				.Select(g => new
				{
					Date = g.Key,
					AvgHumidity = g.Average(w => w.Luftfuktighet) // Räknar ut snittet för dagen
				})
				.OrderBy(x => x.AvgHumidity) // Sorterar stigande (lägst först)
				.Select(x => (x.Date, x.AvgHumidity))
				.ToList();
		}

		// --- G-NIVÅ: MÖGELRISK ---

		// Beräknar ett riskindex för mögel per dag och sorterar från minst till störst risk.
		public List<(DateTime Date, double MoldRisk)> SortDaysByMoldRisk(List<WeatherData> weatherData, string plats)
		{
			return weatherData
				.Where(w => w.Plats == plats)
				.GroupBy(w => w.Datum.Date)
				.Select(g => new
				{
					Date = g.Key,
					// Hämtar dygnsmedel för både temperatur och fuktighet
					AvgTemp = g.Average(w => w.Temp),
					AvgHumidity = g.Average(w => w.Luftfuktighet)
				})
				.Select(x => new
				{
					x.Date,
					// Använder hjälpmetoden för att beräkna riskindexet
					Risk = CalculateMoldIndex(x.AvgTemp, x.AvgHumidity)
				})
				.OrderBy(x => x.Risk) // Sorterar stigande (minst risk först)
				.Select(x => (x.Date, x.Risk))
				.ToList();
		}

		// Hjälpmetod: Beräknar mögelrisk baserat på temperatur och fuktighet.
		// Formeln är förenklad: ((Fukt - 78) * (Temp / 15)).
		// Ingen risk om det är minusgrader eller luftfuktighet under 78%.
		private double CalculateMoldIndex(double temp, double humidity)
		{
			if (temp < 0 || humidity < 78) return 0;

			double risk = ((humidity - 78) * (temp / 15));
			return risk < 0 ? 0 : risk;
		}

		// --- G-NIVÅ: METEOROLOGISKA ÅRSTIDER ---

		// Letar efter första dagen i en sekvens av 5 dagar där temperaturen är under 10 grader (Höst).
		public DateTime? FindMeteorologicalAutumn(List<WeatherData> weatherData)
		{
			// Förbereder en lista med dygnsmedelvärden (Ute) sorterad på datum.
			var dailyTemps = weatherData
				.Where(w => w.Plats == "Ute")
				.GroupBy(w => w.Datum.Date)
				.Select(g => new { Date = g.Key, AvgTemp = g.Average(w => w.Temp) })
				.OrderBy(x => x.Date)
				.ToList();

			// Loopar igenom dagarna för att hitta 5 dagar i rad som uppfyller kravet.
			for (int i = 0; i <= dailyTemps.Count - 5; i++)
			{
				bool isAutumn = true;
				for (int j = 0; j < 5; j++)
				{
					if (dailyTemps[i + j].AvgTemp >= 10.0)
					{
						isAutumn = false;
						break; // Avbryter inre loopen om en dag bryter sekvensen.
					}
				}
				if (isAutumn) return dailyTemps[i].Date; // Returnerar startdatumet för hösten.
			}
			return null;
		}

		// Letar efter första dagen i en sekvens av 5 dagar där temperaturen är 0 grader eller lägre (Vinter).
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
				for (int j = 0; j < 5; j++)
				{
					if (dailyTemps[i + j].AvgTemp > 0.0)
					{
						isWinter = false;
						break;
					}
				}
				if (isWinter) return dailyTemps[i].Date; // Returnerar startdatumet för vintern.
			}
			return null;
		}

		// --- VG-NIVÅ: EXTRA ANALYSER ---

		// Beräknar hur länge balkongdörren varit öppen per dag baserat på temperaturförändringar.
		public List<(DateTime Date, int TotalMinutes)> CalculateBalconyOpenTime(List<WeatherData> weatherData)
		{
			// Delar upp data i Inne/Ute och hanterar eventuella dubbletter genom att ta första värdet per tidpunkt.
			var inneData = weatherData
				.Where(w => w.Plats == "Inne")
				.GroupBy(w => w.Datum)
				.ToDictionary(g => g.Key, g => g.First());

			var uteData = weatherData
				.Where(w => w.Plats == "Ute")
				.GroupBy(w => w.Datum)
				.ToDictionary(g => g.Key, g => g.First());

			var timestamps = inneData.Keys.OrderBy(t => t).ToList();
			var openDoorMinutesPerDay = new Dictionary<DateTime, int>();

			// Jämför temperaturförändringen mellan varje mätpunkt i tidsföljd.
			for (int i = 1; i < timestamps.Count; i++)
			{
				var currentTime = timestamps[i];
				var prevTime = timestamps[i - 1];

				// Ignorerar om det är en lucka i datan (> 15 minuter).
				if ((currentTime - prevTime).TotalMinutes > 15) continue;

				// Kräver att data finns för både inne och ute vid båda tidpunkterna.
				if (inneData.ContainsKey(currentTime) && inneData.ContainsKey(prevTime) &&
					uteData.ContainsKey(currentTime) && uteData.ContainsKey(prevTime))
				{
					double inneDelta = inneData[currentTime].Temp - inneData[prevTime].Temp;
					double uteDelta = uteData[currentTime].Temp - uteData[prevTime].Temp;

					// Logik: Om innertemp sjunker OCH utetemp stiger, antas dörren ha öppnats.
					if (inneDelta < 0 && uteDelta > 0)
					{
						var day = currentTime.Date;
						if (!openDoorMinutesPerDay.ContainsKey(day))
							openDoorMinutesPerDay[day] = 0;

						// Adderar tiden (minuter) till dagens total.
						openDoorMinutesPerDay[day] += (int)(currentTime - prevTime).TotalMinutes;
					}
				}
			}

			// Returnerar resultatet sorterat med flest "öppna minuter" först.
			return openDoorMinutesPerDay
				.Select(kvp => (kvp.Key, kvp.Value))
				.OrderByDescending(x => x.Value)
				.ToList();
		}

		// Sorterar dagar baserat på skillnaden mellan inomhus- och utomhustemperatur.
		public List<(DateTime Date, double AvgDiff)> SortDaysByTempDifference(List<WeatherData> weatherData)
		{
			var inneData = weatherData.Where(w => w.Plats == "Inne");
			var uteData = weatherData.Where(w => w.Plats == "Ute");

			// Matchar ihop inne- och utedata på datum/tid.
			var query = from inne in inneData
						join ute in uteData on inne.Datum equals ute.Datum
						select new
						{
							inne.Datum,
							Diff = Math.Abs(inne.Temp - ute.Temp) // Beräknar absolut skillnad
						};

			// Grupperar skillnaderna per dag och beräknar dygnsmedelvärdet av skillnaden.
			return query
				.GroupBy(x => x.Datum.Date)
				.Select(g => new
				{
					Date = g.Key,
					AvgDiff = g.Average(x => x.Diff)
				})
				.OrderByDescending(x => x.AvgDiff) // Sorterar störst skillnad först
				.Select(x => (x.Date, x.AvgDiff))
				.ToList();
		}
	}
}