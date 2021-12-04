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
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Zongsoft.Common;
using Zongsoft.Configuration.Options;
using Zongsoft.Externals.Wechat.Options;
using Zongsoft.Externals.Wechat.Applets.Options;

namespace Zongsoft.Externals.Wechat.Applets
{
	public class Authentication
	{
		#region 成员字段
		private readonly HttpClient _http;
		private readonly ILogger<Authentication> _logger;
		#endregion

		#region 构造函数
		public Authentication(HttpClient http, ILogger<Authentication> logger)
		{
			_http = http;
			_logger = logger;
			_http.BaseAddress = Urls.BaseAddress;
		}
		#endregion

		#region 公共属性
		[Options("/Externals/Wechat/Applet")]
		public AppletOptions Options { get; set; }
		#endregion

		#region 公共方法
		public async Task<OperationResult<LoginResult>> LoginAsync(string appId, string token)
		{
			if(!this.Options.Apps.TryGetValue(appId, out var app))
				return default;

			var response = await _http.GetAsync($"/sns/jscode2session?appid={appId}&secret={app.Secret}&js_code={token}&grant_type=authorization_code");
			return await response.GetResultAsync<LoginResult>();
		}
		#endregion

		public struct LoginResult
		{
			[Serialization.SerializationMember("session_key")]
			[JsonPropertyName("session_key")]
			public string SessionId { get; set; }

			[Serialization.SerializationMember("openid")]
			[JsonPropertyName("openid")]
			public string OpenId { get; set; }

			[Serialization.SerializationMember("unionid")]
			[JsonPropertyName("unionid")]
			public string UnionId { get; set; }
		}
	}
}
