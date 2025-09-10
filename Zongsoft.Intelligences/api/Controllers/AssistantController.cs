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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
