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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat
{
	internal static class Utility
	{
		private static readonly MD5 _md5 = MD5.Create();

		public static TOptions GetOptions<TOptions>(string path)
		{
			var configuration = Zongsoft.Services.ApplicationContext.Current?.Configuration;
			return configuration == null ? default : Zongsoft.Configuration.ConfigurationBinder.GetOption<TOptions>(configuration, path);
		}

		public static TimeSpan GetPeriod(this DateTime expiry)
		{
			return expiry.Kind == DateTimeKind.Utc ? expiry - DateTime.UtcNow : expiry - DateTime.Now;
		}

		public static async ValueTask<OperationResult<TResult>> GetResultAsync<TResult>(this HttpResponseMessage response, CancellationToken cancellation = default)
		{
			if(response == null)
				throw new ArgumentNullException(nameof(response));

			if(response.IsSuccessStatusCode)
			{
				if(response.Content.Headers.ContentLength <= 0)
					return OperationResult.Success();

				var text = await response.Content.ReadAsStringAsync(cancellation);

				//首先判断返回内容是否为错误信息
				var error = JsonSerializer.Deserialize<ErrorResult>(text, Json.Options);
				if(error.IsFailed)
					return OperationResult.Fail(error.Code, error.Message);

				return OperationResult.Success(JsonSerializer.Deserialize<TResult>(text, Json.Options));
			}
			else
			{
				if(response.Content.Headers.ContentLength <= 0)
					return OperationResult.Fail((int)response.StatusCode, response.ReasonPhrase);

				var error = await response.Content.ReadFromJsonAsync<ErrorResult>(Json.Options, cancellation);
				return OperationResult.Fail(error.Code, error.Message);
			}
		}

		public static string Postmark(IEnumerable<KeyValuePair<string, object>> data, string password, HashAlgorithm algorithm = null)
		{
			if(string.IsNullOrEmpty(password))
				throw new ArgumentNullException(nameof(password));

			if(data == null)
				return null;

			var text = new System.Text.StringBuilder();
			var source = (IEnumerable<KeyValuePair<string, object>>)((data is SortedDictionary<string, object> sorts) ? sorts : data.OrderBy(p => p.Key));

			foreach(var entry in source)
			{
				if(entry.Value == null)
					continue;

				if(text.Length > 0)
					text.Append('&');

				text.Append($"{entry.Key}={entry.Value}");
			}

			if(text.Length > 0)
				text.Append('&');

			text.Append($"key={password}");

			var result = (algorithm ?? _md5).ComputeHash(System.Text.Encoding.UTF8.GetBytes(text.ToString()));
			return System.Convert.ToHexString(result);
		}

		private struct ErrorResult
		{
			#region 构造函数
			public ErrorResult(int code, string message)
			{
				this.Code = code;
				this.Message = message;
			}
			#endregion

			#region 公共属性
			[JsonIgnore]
			[Serialization.SerializationMember(Ignored = true)]
			public bool IsFailed { get => this.Code != 0; }

			[JsonIgnore]
			[Serialization.SerializationMember(Ignored = true)]
			public bool IsSucceed { get => this.Code == 0; }

			/// <summary>获取或设置错误码。</summary>
			[JsonPropertyName("errcode")]
			[Serialization.SerializationMember("errcode")]
			public int Code { get; set; }

			/// <summary>获取或设置错误消息。</summary>
			[JsonPropertyName("errmsg")]
			[Serialization.SerializationMember("errmsg")]
			public string Message { get; set; }
			#endregion

			#region 重写方法
			public override string ToString() => $"[{this.Code}] {this.Message}";
			#endregion
		}
	}
}
