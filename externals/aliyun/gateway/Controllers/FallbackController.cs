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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Common;
using Zongsoft.Components;

namespace Zongsoft.Externals.Aliyun.Gateway.Controllers;

[ApiController]
[Route("Externals/Aliyun/Fallback")]
public class FallbackController : ControllerBase
{
	[HttpPost("{name}/{key?}")]
	public async Task<IActionResult> HandleAsync(string name, string key = null, CancellationToken cancellation = default)
	{
		try
		{
			//Zongsoft.Diagnostics.Logging.GetLogging(this).Debug(await GetRequestInfoAsync());

			var result = await FallbackExecutor.Instance.ExecuteAsync(this.Request, cancellation);
			return result == null ? this.NoContent() : this.Ok(result);
		}
		catch(OperationException ex)
		{
			var result = new { ex.Reason, ex.Message };

			return ex.Reason switch
			{
				nameof(OperationException.Unfound) => this.NotFound(result),
				nameof(OperationException.Unsupported) => this.BadRequest(result),
				nameof(OperationException.Unprocessed) => this.UnprocessableEntity(result),
				nameof(OperationException.Unsatisfied) => this.StatusCode(StatusCodes.Status412PreconditionFailed, result),
				_ => this.StatusCode(StatusCodes.Status500InternalServerError, result),
			};
		}
		catch(AggregateException ae)
		{
			return (IActionResult)ae.Handle<OperationException>(ex => ex.Reason switch
			{
				nameof(OperationException.Unfound) => this.NotFound(new { ex.Reason, ex.Message }),
				nameof(OperationException.Unsupported) => this.BadRequest(new { ex.Reason, ex.Message }),
				nameof(OperationException.Unprocessed) => this.UnprocessableEntity(new { ex.Reason, ex.Message }),
				nameof(OperationException.Unsatisfied) => this.StatusCode(StatusCodes.Status412PreconditionFailed, new { ex.Reason, ex.Message }),
				_ => this.StatusCode(StatusCodes.Status500InternalServerError, new { ex.Reason, ex.Message }),
			});
		}
	}

	private async ValueTask<string> GetRequestInfoAsync()
	{
		this.Request.EnableBuffering();

		var text = new System.Text.StringBuilder();

		text.Append("[" + this.Request.Method + "]");
		text.Append(this.Request.Path.ToString());

		if(this.Request.QueryString.HasValue)
		{
			text.Append('?');
			text.Append(this.Request.QueryString);
		}

		text.AppendLine();

		foreach(var header in this.Request.Headers)
		{
			text.AppendLine(header.Key + ':' + string.Join(';', [.. header.Value]));
		}

		if(this.Request.ContentLength > 0)
		{
			var reader = new StreamReader(this.Request.Body);
			text.AppendLine();
			text.AppendLine(await reader.ReadToEndAsync());
		}

		this.Request.Body.Position = 0;

		return text.ToString();
	}
}
