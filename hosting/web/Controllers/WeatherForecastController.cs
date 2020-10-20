using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Zongsoft.Hosting.Web.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class WeatherForecastController : ControllerBase
	{
		private static readonly string[] Summaries = new[]
		{
			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		[HttpGet]
		[ActionName("Reports")]
		public IEnumerable<WeatherForecast> Get()
		{
			var random = new Random();

			return Enumerable.Range(1, 5).Select(index => new WeatherForecast
			{
				Date = DateTime.Now.AddDays(index),
				TemperatureC = random.Next(-20, 55),
				Summary = Summaries[random.Next(Summaries.Length)]
			});
		}
	}

	public class WeatherForecast
	{
		public DateTime Date { get; set; }
		public int TemperatureC { get; set; }
		public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
		public string Summary { get; set; }
	}
}
