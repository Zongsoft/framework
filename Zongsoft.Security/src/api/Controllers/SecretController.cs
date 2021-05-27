/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Web;
using Zongsoft.Services;
using Zongsoft.Security.Membership;

namespace Zongsoft.Security.Web.Controllers
{
	[ApiController]
	[Area(Modules.Security)]
	[Route("{area}/{controller}")]
	public class SecretController : ControllerBase
	{
		[ServiceDependency]
		public ISecretor Secretor { get; set; }

		[HttpGet("{action}/{token}")]
		[HttpGet("{token}/{action}")]
		public IActionResult Exists(string token)
		{
			return this.Secretor.Exists(token) ? this.NoContent() : this.NotFound();
		}

		[HttpPost("{template}/{destination}")]
		public IActionResult Transmit(string template, string destination, [FromQuery]string channel)
		{
			if(string.IsNullOrEmpty(template))
				return this.BadRequest();
			if(string.IsNullOrEmpty(destination))
				return this.BadRequest();

			var token = this.Secretor.Transmitter.Transmit(destination, template, channel);
			return string.IsNullOrEmpty(token) ? this.NotFound() : this.Content(token);
		}
	}
}
