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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using Microsoft.AspNetCore.Mvc.Infrastructure;

using Zongsoft.Data;
using Zongsoft.Common;
using Zongsoft.Security;
using Zongsoft.Security.Membership;

namespace Zongsoft.Web.Filters;

public class ExceptionFilter : IExceptionFilter
{
	public void OnException(ExceptionContext context)
	{
		switch(context.Exception)
		{
			case AuthenticationException authentication:
				context.HttpContext.Response.Headers.Append("X-Security-Error", authentication.Reason);
				context.Result = new ObjectResult(GetProblem(context, StatusCodes.Status401Unauthorized, authentication.Reason));
				break;
			case SecurityException securityException:
				context.HttpContext.Response.Headers.Append("X-Security-Error", securityException.Reason);
				context.Result = new ObjectResult(GetProblem(context, StatusCodes.Status403Forbidden, securityException.Reason));
				break;
			case OperationException operationException when operationException.IsArgument:
				context.Result = new ObjectResult(GetProblem(context, StatusCodes.Status400BadRequest));
				break;
			case OperationException operationException when operationException.IsUnfound:
				context.Result = new ObjectResult(GetProblem(context, StatusCodes.Status404NotFound));
				break;
			case OperationException operationException when operationException.IsUnsatisfied:
				context.Result = new ObjectResult(GetProblem(context, StatusCodes.Status412PreconditionFailed));
				break;
			case OperationException operationException when operationException.IsUnprocessed:
				context.Result = new ObjectResult(GetProblem(context, StatusCodes.Status406NotAcceptable));
				break;
			case OperationException operationException when operationException.IsUnsupported:
				context.Result = new ObjectResult(GetProblem(context, StatusCodes.Status405MethodNotAllowed));
				break;
			case DataArgumentException argumentException:
				if(!string.IsNullOrEmpty(argumentException.Name))
					context.ModelState.AddModelError(argumentException.Name, argumentException.Message);

				context.Result = new ObjectResult(GetProblem(context, StatusCodes.Status400BadRequest));
				break;
			case DataConflictException conflictException:
				if(!string.IsNullOrEmpty(conflictException.Key))
					context.ModelState.AddModelError(conflictException.Key, conflictException.Message);

				var problem = GetProblem(context, StatusCodes.Status409Conflict);

				if(conflictException.Fields != null && conflictException.Fields.Length > 0)
					problem.Extensions.Add(nameof(DataConflictException.Fields), conflictException.Fields);

				context.Result = new ObjectResult(problem);
				break;
			case DataConstraintException constraintException:
				if(!string.IsNullOrEmpty(constraintException.Field))
					context.ModelState.AddModelError(constraintException.Field, constraintException.Message);

				context.Result = new ObjectResult(GetProblem(context, StatusCodes.Status409Conflict));
				break;
			case DataOperationException:
				context.Result = new ObjectResult(GetProblem(context, StatusCodes.Status412PreconditionFailed));
				break;
			case NotSupportedException:
			case NotImplementedException:
				context.Result = new ObjectResult(GetProblem(context, StatusCodes.Status501NotImplemented));
				break;
			default:
				context.Result = new ObjectResult(GetProblem(context, StatusCodes.Status500InternalServerError));
				Zongsoft.Diagnostics.Logger.GetLogger(this).Error(context.Exception);
				break;
		}
	}

	private static ProblemDetails GetProblem(ExceptionContext context, int status, params ReadOnlySpan<KeyValuePair<string, object>> extras) => GetProblem(context, status, null, extras);
	private static ProblemDetails GetProblem(ExceptionContext context, int status, string title, params ReadOnlySpan<KeyValuePair<string, object>> extras)
	{
		var errors = context.ModelState;
		var factory = (ProblemDetailsFactory)context.HttpContext.RequestServices.GetService(typeof(ProblemDetailsFactory));

		var result = factory != null ?
		(
			errors == null || errors.IsValid ?
			factory.CreateProblemDetails(context.HttpContext, status, title, context.Exception.GetType().Name, context.Exception.Message) :
			factory.CreateValidationProblemDetails(context.HttpContext, errors, status, title, context.Exception.GetType().Name, context.Exception.Message)
		):
		(
			errors == null || errors.IsValid ?
			new ProblemDetails()
			{
				Status = status,
				Title = title,
				Type = context.Exception.GetType().Name,
				Detail = context.Exception.Message,
			} :
			new ValidationProblemDetails(errors)
			{
				Status = status,
				Title = title,
				Type = context.Exception.GetType().Name,
				Detail = context.Exception.Message,
			}
		);

		if(extras != null && extras.Length > 0)
		{
			foreach(var extra in extras)
				result.Extensions.Add(extra.Key, extra.Value);
		}

		return result;
	}
}
