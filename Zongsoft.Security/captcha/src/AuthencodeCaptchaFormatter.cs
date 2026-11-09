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
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Services;

namespace Zongsoft.Security.Captcha;

[Service<ICaptchaFormatter<HttpContext>>]
public class AuthencodeCaptchaFormatter : ICaptchaFormatter<HttpContext>, IMatchable, IMatchable<string>
{
	#region 公共属性
	public string Scheme => "Authencode";
	#endregion

	#region 公共方法
	public ValueTask<object> FormatAsync(HttpContext context, object value, CancellationToken cancellation = default)
	{
		if(value is AuthencodeCaptcha.AuthencodeCaptchaResult data && data.HasValue)
		{
			context.Response.Headers[Zongsoft.Web.Http.Headers.Captcha] = data.Token;
			return ValueTask.FromResult<object>(new FileContentResult(data.Data, data.Type));
		}

		return ValueTask.FromResult<object>(null);
	}
	#endregion

	#region 服务匹配
	bool IMatchable<string>.Match(string argument) => this.Scheme.Equals(argument, StringComparison.OrdinalIgnoreCase);
	bool IMatchable.Match(object argument) => argument is string scheme && this.Scheme.Equals(scheme, StringComparison.OrdinalIgnoreCase);
	#endregion
}