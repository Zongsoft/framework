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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Web;
using Zongsoft.Web.Http;

namespace Zongsoft.Intelligences.Web.Controllers;

partial class AssistantController
{
	[ControllerName("Models")]
	public class ModelController : ControllerBase
	{
		[HttpGet("/[area]/{name}/[controller]/{id?}")]
		public async ValueTask<IActionResult> GetAsync(string name, string id = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				return this.BadRequest($"Unspecified the AI assistant.");

			var assistant = AssistantManager.GetAssistant(name);
			if(assistant == null)
				return this.NotFound($"The specified '{name}' AI assistant was not found.");

			if(string.IsNullOrEmpty(id))
				return this.Ok(assistant.Modeling.GetModelsAsync(null, cancellation));

			var model = await assistant.Modeling.GetModelAsync(id, cancellation);
			return model == null ? this.NotFound() : this.Ok(model);
		}

		[HttpPost("/[area]/{name}/[controller]/{id}")]
		public async ValueTask<IActionResult> ActivateAsync(string name, string id = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				return this.BadRequest($"Unspecified the AI assistant.");
			if(string.IsNullOrEmpty(id))
				return this.BadRequest($"Unspecified the model identifier.");

			var assistant = AssistantManager.GetAssistant(name);
			if(assistant == null)
				return this.NotFound($"The specified '{name}' AI assistant was not found.");

			return (await assistant.Modeling.ActivateAsync(id, cancellation)) ?
				this.NoContent() :
				this.NotFound();
		}
	}
}