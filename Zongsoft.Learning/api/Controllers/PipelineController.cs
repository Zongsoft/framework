using System;

using Microsoft.AspNetCore.Mvc;

namespace Zongsoft.Learning.Web.Controllers;

[Area("MachineLearning")]
public class PipelineController : ControllerBase
{
	[HttpGet]
	public IActionResult Get()
	{
		return this.Ok(new
		{
			Pipeline.Catalog.Catalogs,
			Pipeline.Catalog.Trainers,
		});
	}
}
