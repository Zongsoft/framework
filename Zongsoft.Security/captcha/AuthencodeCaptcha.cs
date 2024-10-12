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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

using Zongsoft.Services;
using Zongsoft.Collections;

namespace Zongsoft.Security.Captcha
{
	[Service<ICaptcha>]
	public class AuthencodeCaptcha(IServiceProvider serviceProvider) : ICaptcha, IMatchable, IMatchable<string>
	{
		#region 成员字段
		private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		#endregion

		#region 公共属性
		public string Scheme => "Authencode";
		#endregion

		#region 公共方法
		public async ValueTask<object> IssueAsync(object argument, Parameters parameters, CancellationToken cancellation = default)
		{
		}

		public async ValueTask<string> VerifyAsync(object argument, Parameters parameters, CancellationToken cancellation = default)
		{
			return null;
		}

		public async ValueTask<bool> VerifyAsync(string token, CancellationToken cancellation)
		{
			if(string.IsNullOrEmpty(token))
				return false;

			return false;
		}
		#endregion

		#region 服务匹配
		bool IMatchable<string>.Match(string argument) => this.Scheme.Equals(argument, StringComparison.OrdinalIgnoreCase);
		bool IMatchable.Match(object argument) => argument is string scheme && this.Scheme.Equals(scheme, StringComparison.OrdinalIgnoreCase);
		#endregion
	}
}