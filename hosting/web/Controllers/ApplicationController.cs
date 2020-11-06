using System;
using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Zongsoft.Hosting.Web.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class ApplicationController : ControllerBase
	{
		[HttpGet]
		public IActionResult Get()
		{
			var applicationContext = Zongsoft.Services.ApplicationContext.Current;

			if(applicationContext == null)
				return this.NoContent();

			return this.Ok(new
			{
				applicationContext.Name,
				applicationContext.Title,
				applicationContext.Description,
				Environment = applicationContext.Environment.Name,
			});
		}

		[HttpGet("Modules")]
		public IActionResult GetModules()
		{
			var applicationContext = Zongsoft.Services.ApplicationContext.Current;

			if(applicationContext == null)
				return this.NoContent();

			return this.Ok(applicationContext.Modules.Select(module => new
			{
				module.Name,
				module.Title,
				module.Description,
			}));
		}
	}
}
