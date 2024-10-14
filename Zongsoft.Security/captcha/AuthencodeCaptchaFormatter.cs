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
 * Copyright (C) 2015-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Security.Captcha library.
 *
 * The Zongsoft.Security.Captcha is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security.Captcha is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security.Captcha library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Services;

namespace Zongsoft.Security.Captcha
{
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
}