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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Zongsoft.Security.Privileges;

namespace Zongsoft.Externals.Wechat.Web;

internal static class WebUtility
{
	public static ValueTask<bool> AuthorizeAccountAsync(HttpContext context, Account account, CancellationToken cancellation) => AuthorizeAsync(context, account, "Wechat.Accounts", cancellation);

	public static async ValueTask<bool> AuthorizeAsync(HttpContext context, object resource, string name, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		var provider = context.RequestServices.GetService(typeof(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider)) as Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider;
		var authorization = context.RequestServices.GetService(typeof(Microsoft.AspNetCore.Authorization.IAuthorizationService)) as Microsoft.AspNetCore.Authorization.IAuthorizationService;
		var policy = provider == null ? null : await provider.GetPolicyAsync(name);

		if(policy == null || authorization == null)
			return false;

		var result = await Microsoft.AspNetCore.Authorization.AuthorizationServiceExtensions.AuthorizeAsync(authorization, context.User, resource, policy);
		cancellation.ThrowIfCancellationRequested();
		return result.Succeeded;
	}

	public static async ValueTask<IActionResult> LoginAsync(HttpContext context, Account account, string scenario, CancellationToken cancellation)
	{
		if(string.IsNullOrWhiteSpace(account.Code) || string.IsNullOrWhiteSpace(scenario) || account.Code.IndexOfAny([':', '=', '?', '/', '(', ')']) >= 0)
			return new BadRequestResult();
		if(!await WebUtility.AuthorizeAccountAsync(context, account, cancellation))
			return new StatusCodeResult(StatusCodes.Status403Forbidden);

		var code = await context.Request.ReadAsStringAsync(cancellation);
		if(string.IsNullOrWhiteSpace(code))
			return new BadRequestResult();

		try
		{
			var principal = await Authentication.AuthenticateAsync("Wechat", account.ToString(), code, scenario, new(), cancellation);
			return principal == null ? new StatusCodeResult(StatusCodes.Status403Forbidden) : new OkObjectResult(Authentication.Transformer.Transform(principal));
		}
		catch(AuthenticationException)
		{
			return new StatusCodeResult(StatusCodes.Status403Forbidden);
		}
	}

	public static async ValueTask<string> ReadAsStringAsync(this HttpRequest request, CancellationToken cancellation = default)
	{
		if(request == null)
			throw new ArgumentNullException(nameof(request));

		cancellation.ThrowIfCancellationRequested();

		var encoding = MediaTypeHeaderValue.TryParse(request.ContentType, out MediaTypeHeaderValue mediaType) ?
			(mediaType.Encoding ?? Encoding.UTF8) : Encoding.UTF8;

		using(var reader = new StreamReader(
			request.Body,
			encoding,
			detectEncodingFromByteOrderMarks: true,
			bufferSize: 1024,
			leaveOpen: true))
		{
			return await reader.ReadToEndAsync(cancellation);
		}
	}
}
