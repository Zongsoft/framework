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
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authentication;

using Zongsoft.Data;
using Zongsoft.Security;
using Zongsoft.Security.Membership;

namespace Zongsoft.Web.Filters
{
	public class ExceptionFilter : IExceptionFilter
	{
		public void OnException(ExceptionContext context)
		{
			if(context.Exception is AuthenticationException authentication)
			{
				context.HttpContext.Response.Headers.Add("X-Security-Reason", authentication.Reason.ToString());
				context.Result = new UnauthorizedResult();
			}
			else if(context.Exception is SecurityException securityException)
			{
				var properties = new AuthenticationProperties();

				properties.Items.Add(nameof(SecurityException.Reason), securityException.Reason);
				properties.Parameters.Add(nameof(SecurityException.Reason), securityException.Reason);

				context.Result = new ForbidResult(properties);
			}
			else if(context.Exception is DataConflictException conflictException)
			{
				context.ModelState.AddModelError(conflictException.Key, conflictException.Message);

				var problem = new ProblemDetails()
				{
					Status = StatusCodes.Status409Conflict,
					Type = conflictException.Key,
					Detail = conflictException.Message,
				};

				if(conflictException.Fields != null && conflictException.Fields.Length > 0)
				{
					context.HttpContext.Response.Headers.Add("X-Conflict-Fields", string.Join(',', conflictException.Fields));
					problem.Extensions.Add(nameof(DataConflictException.Fields), conflictException.Fields);
				}

				context.Result = new ConflictObjectResult(problem);
			}
		}
	}
}
