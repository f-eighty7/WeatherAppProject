using Microsoft.EntityFrameworkCore;
using Core;

namespace DataAccess
{
	public class WeatherContext : DbContext
	{
		public DbSet<WeatherData> WeatherData { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				// Konfigurerar SQL Server (LocalDB) och anger databasnamnet "WeatherDB".
				optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=WeatherDB;Trusted_Connection=True;");
			}
		}
	}
}