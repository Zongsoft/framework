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
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Security;
using Zongsoft.Services;

namespace Zongsoft.Externals.Wechat.Paying
{
	[Service]
	public class PaymentService
	{
		#region 构造函数
		public PaymentService(IAccount account)
		{
			this.Account = account ?? throw new ArgumentNullException(nameof(account));
		}
		#endregion

		#region 公共属性
		public IAccount Account { get; }
		#endregion

		#region 公共方法
		public async Task<OperationResult<string>> PayAsync(PaymentRequest request, Scenario scenario, CancellationToken cancellation = default)
		{
			if(request == null)
				throw new ArgumentNullException(nameof(request));

			var client = this.Account.GetHttpClient();
			var response = await client.PostAsJsonAsync(GetUrl(request.GetType().Name, "transactions", scenario), request, cancellation);

			if(response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<PayResult>(null, cancellation);
				return OperationResult.Success(result.Code);
			}

			return OperationResult.Fail((int)response.StatusCode, response.ReasonPhrase, await response.Content.ReadAsStringAsync(cancellation));
		}

		public async Task<OperationResult> Cancel(string voucher, Scenario scenario, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(voucher))
				throw new ArgumentNullException(nameof(voucher));

			var client = this.Account.GetHttpClient();
			var response = await client.PostAsJsonAsync(GetUrl(null, $"transactions/out-trade-no/{voucher}/close", scenario), new { mchid = this.Account.Code }, cancellation);

			if(response.IsSuccessStatusCode)
				return OperationResult.Success();

			return OperationResult.Fail((int)response.StatusCode, response.ReasonPhrase, await response.Content.ReadAsStringAsync(cancellation));
		}

		public async Task<OperationResult> Cancel(string subsidiary, string voucher, Scenario scenario, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(voucher))
				throw new ArgumentNullException(nameof(voucher));

			var client = this.Account.GetHttpClient();
			var response = await client.PostAsJsonAsync(GetUrl("Broker", $"transactions/out-trade-no/{voucher}/close", scenario), new { sp_mchid = this.Account.Code, sub_mchid = subsidiary }, cancellation);

			if(response.IsSuccessStatusCode)
				return OperationResult.Success();

			return OperationResult.Fail((int)response.StatusCode, response.ReasonPhrase, await response.Content.ReadAsStringAsync(cancellation));
		}

		public async Task<OperationResult<T>> GetAsync<T>(string voucher, Scenario scenario, CancellationToken cancellation = default) where T : PaymentOrder
		{
			if(string.IsNullOrEmpty(voucher))
				throw new ArgumentNullException(nameof(voucher));

			var client = this.Account.GetHttpClient();
			var response = await client.GetAsync(GetUrl(typeof(T).Name, $"transactions/out-trade-no/{voucher}", scenario), cancellation);

			if(response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<T>(null, cancellation);
				return OperationResult.Success(result);
			}

			return OperationResult.Fail((int)response.StatusCode, response.ReasonPhrase, await response.Content.ReadAsStringAsync(cancellation));
		}

		public async Task<OperationResult<T>> GetCompletedAsync<T>(string id, Scenario scenario, CancellationToken cancellation = default) where T : PaymentOrder
		{
			if(string.IsNullOrEmpty(id))
				throw new ArgumentNullException(nameof(id));

			var client = this.Account.GetHttpClient();
			var response = await client.GetAsync(GetUrl(typeof(T).Name, $"transactions/id/{id}", scenario), cancellation);

			if(response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<T>(null, cancellation);
				return OperationResult.Success(result);
			}

			return OperationResult.Fail((int)response.StatusCode, response.ReasonPhrase, await response.Content.ReadAsStringAsync(cancellation));
		}
		#endregion

		#region 私有方法
		private static string GetUrl(string mode, string path, Scenario scenario)
		{
			if(mode != null && mode.EndsWith("Broker"))
				path = "partner/" + path;

			return scenario switch
			{
				Scenario.App => $"{path}/app",
				Scenario.Web => $"{path}/jsapi",
				Scenario.Native => "{path}/native",
				_ => $"{path}/jsapi",
			};
		}
		#endregion

		#region 嵌套子类
		private struct PayResult
		{
			[System.Text.Json.Serialization.JsonPropertyName("prepay_id")]
			public string Code { get; set; }
		}
		#endregion
	}
}
