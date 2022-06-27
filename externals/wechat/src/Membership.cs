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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Reflection;
using Zongsoft.Serialization;

namespace Zongsoft.Externals.Wechat
{
	public class Membership
	{
		#region 静态变量
		private static readonly HttpClient _http;
		#endregion

		#region 静态函数
		static Membership()
		{
			_http = new HttpClient();
			_http.BaseAddress = new Uri("https://api.mch.weixin.qq.com/v3");
			_http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			_http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Zongsoft.Externals.Wechat", "1.0"));
		}
		#endregion

		#region 构造函数
		public Membership(Account account)
		{
			if(account.IsEmpty)
				throw new ArgumentNullException(nameof(account));

			this.Account = account;
			this.Templates = new TemplateService(account);
		}
		#endregion

		#region 公共属性
		public Account Account { get; }
		public TemplateService Templates { get; }
		#endregion

		#region 公共方法
		public async ValueTask<OperationResult<IEnumerable<string>>> AllocateAsync(string templateId, IEnumerable<string> codes, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(templateId))
				throw new ArgumentNullException(nameof(templateId));

			var response = await _http.PostAsJsonAsync($"marketing/membercard-open/cards/{templateId}/codes/deposit", new { code = codes }, cancellation);

			if(response.IsSuccessStatusCode)
			{
				var result = await response.GetResultAsync<AllocateResult>(cancellation);

				if(result.Value.HasData(out var data))
					OperationResult.Success(data.Where(entry => string.Equals(entry.Result, "SUCCESS", StringComparison.OrdinalIgnoreCase)));
			}

			return OperationResult.Fail(response.StatusCode.ToString());
		}

		public async ValueTask<OperationResult> ObsoleteAsync(string templateId, string code, CancellationToken cancellation = default)
		{
			return OperationResult.Fail();
		}
		#endregion

		#region 嵌套结构
		private struct AllocateResult
		{
			public Entry[] Data;
			public bool HasData(out Entry[] data)
			{
				data = this.Data;
				return data != null && data.Length > 0;
			}

			public struct Entry
			{
				public string Code;
				public string Result;
			}
		}
		#endregion

		#region 嵌套服务
		public class TemplateService
		{
			private readonly Account _account;
			internal TemplateService(Account account) => _account = account;
		}
		#endregion
	}
}
