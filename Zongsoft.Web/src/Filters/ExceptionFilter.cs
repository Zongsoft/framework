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
using System.Linq;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

using Zongsoft.Data;
using Zongsoft.Data.Metadata;
using Zongsoft.Common;
using Zongsoft.Security;
using Zongsoft.Security.Privileges;

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
				if(!string.IsNullOrEmpty(conflictException.Name))
					context.ModelState.AddModelError(conflictException.Name, conflictException.Message);

				context.Result = new ObjectResult(GetProblem(context, StatusCodes.Status409Conflict));
				break;
			case DataConstraintException constraintException:
				if(!string.IsNullOrEmpty(constraintException.Name))
					context.ModelState.AddModelError(constraintException.Name, constraintException.Message);

				var problem = GetProblem(context, StatusCodes.Status409Conflict);
				var principal = Translate(constraintException.Principal);
				var foreigner = Translate(constraintException.Foreigner);

				if(constraintException.Principal != null)
				{
					if(principal.IsEmpty)
						problem.Extensions.Add(nameof(constraintException.Principal), constraintException.Principal);
					else
						problem.Extensions.Add(nameof(constraintException.Principal), principal);

					problem.Title = problem.Detail = string.Format(Properties.Resources.ConstraintException_Unique_Message, principal.GetTitle(), principal.GetFields(), constraintException.Value);
				}

				if(constraintException.Foreigner != null)
				{
					if(foreigner.IsEmpty)
						problem.Extensions.Add(nameof(constraintException.Foreigner), constraintException.Foreigner);
					else
						problem.Extensions.Add(nameof(constraintException.Foreigner), foreigner);

					problem.Title = problem.Detail = string.Format(Properties.Resources.ConstraintException_Dependency_Message, principal.GetTitle(), principal.GetFields(), foreigner.GetTitle(), foreigner.GetFields(), constraintException.Value);
				}

				context.Result = new ObjectResult(problem);
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
				Zongsoft.Diagnostics.Logging.GetLogging(this).Error(context.Exception);
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

		if(!extras.IsEmpty)
		{
			foreach(var extra in extras)
				result.Extensions.Add(extra.Key, extra.Value);
		}

		return result;
	}

	private static ConstraintDescriptor Translate(DataConstraintException.Actor actor)
	{
		if(actor == null || string.IsNullOrEmpty(actor.Name))
			return default;

		var entities = Mapping.Entities.Find(actor.Name);

		if(entities == null || entities.Length == 0)
			return default;

		for(int i = 0; i < entities.Length; i++)
		{
			var fields = new List<ConstraintDescriptor.Field>(actor.Fields.Length);

			for(int j = 0; j < actor.Fields.Length; j++)
			{
				if(entities[i].Properties.TryGetValue(actor.Fields[j], out var property))
					fields.Add(new ConstraintDescriptor.Field(property.Name, property.GetLabel()));
			}

			if(fields.Count == actor.Fields.Length)
				return new ConstraintDescriptor(entities[i].Name, entities[i].GetTitle(), fields);
		}

		return default;
	}

	private readonly struct ConstraintDescriptor
	{
		public ConstraintDescriptor(string name, params IEnumerable<Field> fields) : this(name, null, fields) { }
		public ConstraintDescriptor(string name, string description, params IEnumerable<Field> fields)
		{
			this.Name = name;
			this.Description = description;
			this.Fields = fields;
		}

		public readonly string Name;
		public readonly string Description;
		public readonly IEnumerable<Field> Fields;

		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public readonly bool IsEmpty => string.IsNullOrEmpty(this.Name);

		public string GetTitle() => string.IsNullOrWhiteSpace(this.Description) ? this.Name : this.Description;
		public string GetFields(string separator = null) => this.Fields == null ? null : string.Join(string.IsNullOrEmpty(separator) ? Properties.Resources.Separator : separator, this.Fields.Select(field => string.IsNullOrWhiteSpace(field.Label) ? field.Name : field.Label));
		public override string ToString() => this.Name;

		public readonly struct Field(string name, string label = null)
		{
			public readonly string Name = name;
			public readonly string Label = label;
			public override string ToString() => this.Name;
		}
	}
}
