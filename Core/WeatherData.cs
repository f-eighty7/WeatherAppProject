using System;
using System.ComponentModel.DataAnnotations; // Behövs för [Key]

namespace Core
{
	public class WeatherData
	{
		[Key] // Säger åt databasen att detta är ID-numret
		public int Id { get; set; }

		public DateTime Datum { get; set; }

		public string Plats { get; set; } // "Ute" eller "Inne"

		public double Temp { get; set; } // Temperatur (t.ex. 10.5)

		public int Luftfuktighet { get; set; } // Luftfuktighet (t.ex. 45)
	}
}