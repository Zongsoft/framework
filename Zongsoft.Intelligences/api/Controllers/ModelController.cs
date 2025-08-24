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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Intelligences.Web library.
 *
 * The Zongsoft.Intelligences.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Intelligences.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Intelligences.Web library. If not, see <http://www.gnu.org/licenses/>.
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

partial class CopilotController
{
	[ControllerName("Models")]
	public class ModelController : ControllerBase
	{
		[HttpGet("/[area]/{name}/[controller]/{id?}")]
		public async ValueTask<IActionResult> GetAsync(string name, string id = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				return this.BadRequest($"Unspecified the AI assistant.");

			var copilot = CopilotManager.GetCopilot(name);
			if(copilot == null)
				return this.NotFound($"The specified '{name}' AI assistant was not found.");

			if(string.IsNullOrEmpty(id))
				return this.Ok(copilot.Modeling.GetModelsAsync(null, cancellation));

			var model = await copilot.Modeling.GetModelAsync(id, cancellation);
			return model == null ? this.NotFound() : this.Ok(model);
		}

		[HttpPost("/[area]/{name}/[controller]/{id}")]
		public async ValueTask<IActionResult> ActivateAsync(string name, string id = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				return this.BadRequest($"Unspecified the AI assistant.");
			if(string.IsNullOrEmpty(id))
				return this.BadRequest($"Unspecified the model identifier.");

			var copilot = CopilotManager.GetCopilot(name);
			if(copilot == null)
				return this.NotFound($"The specified '{name}' AI assistant was not found.");

			return (await copilot.Modeling.ActivateAsync(id, cancellation)) ?
				this.NoContent() :
				this.NotFound();
		}
	}
}