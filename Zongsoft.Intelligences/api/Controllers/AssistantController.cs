/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Web;

namespace Zongsoft.Intelligences.Web.Controllers;

[Area("AI")]
[ControllerName("Assistants")]
public partial class AssistantController : ControllerBase
{
	[HttpGet("{name?}")]
	public IActionResult Get(string name = null)
	{
		if(string.IsNullOrEmpty(name))
		{
			var results = AssistantManager.GetAssistants();

			if(results != null || results.Any())
				return this.Ok(results.Select(Map));

			return this.NoContent();
		}

		var assistant = AssistantManager.GetAssistant(name);

		return assistant == null ?
			this.NotFound() :
			this.Ok(Map(assistant));

		static object Map(IAssistant assistant) => new
		{
			assistant.Name,
			assistant.Driver,
			assistant.Description,
		};
	}
}
