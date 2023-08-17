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
 * The MIT License (MIT)
 * 
 * Copyright (C) 2015-2023 Zongsoft Corporation <http://www.zongsoft.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.ComponentModel;

namespace Zongsoft.Hosting.Web.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class ApplicationController : ControllerBase
	{
		[HttpGet]
		public IActionResult Get()
		{
			var applicationContext = ApplicationContext.Current;
			if(applicationContext == null)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			return this.Ok(new
			{
				applicationContext.Name,
				applicationContext.Title,
				applicationContext.Description,
				Environment = applicationContext.Environment.Name,
			});
		}

		[HttpGet("Modules/{module?}")]
		public IActionResult GetModules(string module = null)
		{
			var applicationContext = ApplicationContext.Current;
			if(applicationContext == null)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			if(string.IsNullOrEmpty(module) || module == "*")
				return this.Ok(applicationContext.Modules.Select(GetApplicationModule));

			if(applicationContext.Modules.TryGet(module, out var applicationModule))
				return this.Ok(GetApplicationModule(applicationModule));
			else
				return this.NotFound();

			static object GetApplicationModule(IApplicationModule module)
			{
				if(module == null)
					return null;

				return new
				{
					module.Name,
					module.Title,
					module.Description,
					Events = module.GetEvents().Select(@event => new
					{
						Name = @event.QualifiedName,
						@event.Title,
						@event.Description,
					}),
					Targets = module.Schemas,
				};
			}
		}

		[HttpGet("Modules/{module}/Targets/{targets?}")]
		public IActionResult GetTargets(string module, string targets = null)
		{
			var applicationContext = ApplicationContext.Current;
			if(applicationContext == null)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			if(module == "*")
			{
				var result = string.IsNullOrWhiteSpace(targets) || targets == "*" ?
					applicationContext.Modules.Select(m => new
					{
						Module = m.Name,
						Targets = (IEnumerable<Schema>)m.Schemas,
					}):
					applicationContext.Modules.Select(m => new
					{
						Module = m.Name,
						Targets = m.Schemas.Where(target => targets.Split(',', StringSplitOptions.TrimEntries).Contains(target.Name, StringComparer.OrdinalIgnoreCase)),
					});

				return this.Ok(result);
			}

			if(module == null || module == "_")
				module = string.Empty;

			if(applicationContext.Modules.TryGet(module, out var applicationModule))
			{
				if(string.IsNullOrEmpty(targets) || targets == "*")
					return this.Ok(applicationModule.Schemas);

				var array = targets.Split(',', StringSplitOptions.TrimEntries);
				var result = applicationModule.Schemas
					.Where(target => array.Contains(target.Name, StringComparer.OrdinalIgnoreCase));

				if(result == null || !result.Any())
					return this.NoContent();

				return array.Length == 1 ? this.Ok(result.FirstOrDefault()) : this.Ok(result);
			}

			return this.NotFound();
		}
	}
}
