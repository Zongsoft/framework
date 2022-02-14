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
			ValidationProblemDetails problem;

			switch(context.Exception)
			{
				case AuthenticationException authentication:
					context.HttpContext.Response.Headers.Add("X-Security-Error", authentication.Reason.ToString());
					context.Result = new ObjectResult(new ValidationProblemDetails(context.ModelState)
					{
						Title = authentication.Reason,
						Detail = authentication.Message,
						Status = StatusCodes.Status401Unauthorized,
					});

					break;
				case SecurityException securityException:
					context.HttpContext.Response.Headers.Add("X-Security-Error", securityException.Reason.ToString());
					context.Result = new ObjectResult(new ValidationProblemDetails(context.ModelState)
					{
						Title = securityException.Reason,
						Detail = securityException.Message,
						Status = StatusCodes.Status403Forbidden,
					});

					break;
				case DataArgumentException argumentException:
					if(!string.IsNullOrEmpty(argumentException.Name))
						context.ModelState.AddModelError(argumentException.Name, argumentException.Message);

					context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
					{
						Detail = argumentException.Message
					});

					break;
				case DataConflictException conflictException:
					if(!string.IsNullOrEmpty(conflictException.Key))
						context.ModelState.AddModelError(conflictException.Key, conflictException.Message);

					problem = new ValidationProblemDetails(context.ModelState)
					{
						Detail = conflictException.Message,
					};

					if(conflictException.Fields != null && conflictException.Fields.Length > 0)
						problem.Extensions.Add(nameof(DataConflictException.Fields), conflictException.Fields);

					context.Result = new ConflictObjectResult(problem);
					break;
				case DataConstraintException constraintException:
					if(!string.IsNullOrEmpty(constraintException.Field))
						context.ModelState.AddModelError(constraintException.Field, constraintException.Message);

					context.Result = new ObjectResult(new ValidationProblemDetails(context.ModelState)
					{
						Status = StatusCodes.Status409Conflict,
						Detail = constraintException.Message
					});

					break;
				case DataOperationException operationException:
					context.Result = new ObjectResult(new ValidationProblemDetails(context.ModelState)
					{
						Status = StatusCodes.Status412PreconditionFailed,
						Detail = operationException.Message,
					});

					break;
				case NotSupportedException unsupported:
				case NotImplementedException unimplemented:
					context.Result = new StatusCodeResult(StatusCodes.Status405MethodNotAllowed);
					break;
				default:
					Zongsoft.Diagnostics.Logger.Error(context.Exception);
					break;
			}
		}
	}
}
