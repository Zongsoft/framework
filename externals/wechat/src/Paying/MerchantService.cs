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
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public partial class MerchantService
	{
		#region 成员字段
		private HttpClient _client;
		#endregion

		#region 构造函数
		internal MerchantService(IAuthority authority)
		{
			this.Authority = authority ?? throw new ArgumentNullException(nameof(authority));
			this.Contracts = new ContractService(this);
		}
		#endregion

		#region 公共属性
		public IAuthority Authority { get; }
		public ContractService Contracts { get; }
		#endregion

		#region 保护属性
		protected HttpClient Client
		{
			get
			{
				if(_client != null)
					return _client;

				lock(this)
				{
					if(_client == null)
						_client = HttpClientFactory.GetHttpClient(this.Authority);
				}

				return _client;
			}
		}
		#endregion

		#region 公共方法
		/// <summary>提交平台收付通二级商户进件申请；不用于普通服务商特约商户进件。</summary>
		/// <param name="request">待提交的二级商户资料。</param>
		/// <param name="cancellation">操作取消标记。</param>
		/// <returns>微信支付申请单号。</returns>
		public async ValueTask<string> RegisterAsync(Registration request, CancellationToken cancellation = default)
		{
			ArgumentNullException.ThrowIfNull(request);
			ValidateRegistration(request);
			var certificate = await this.Authority.GetCertificateAsync(cancellation);
			var result = await this.Client.PostAsync<Registration, RegistrationResult>("ecommerce/applyments/", request, certificate, cancellation);

			if(result.ApplymentId <= 0 || result.RequestId != request.RequestId)
				throw new FormatException();

			return result.ApplymentId.ToString(System.Globalization.CultureInfo.InvariantCulture);
		}

		public async ValueTask<Applyment> GetAsync(string key, CancellationToken cancellation = default)
		{
			ValidateRegistrationText(key, 124, nameof(key));
			var result = await this.Client.GetAsync<Applyment>($"ecommerce/applyments/out-request-no/{Uri.EscapeDataString(key)}", cancellation);
			if(result == null || result.RequestId != key || result.ApplymentId <= 0 || string.IsNullOrWhiteSpace(result.Status) ||
				result.Status == "FINISH" && string.IsNullOrWhiteSpace(result.MerchantId))
				throw new FormatException();

			var account = result.Account;
			account.Name = Decrypt(account.Name);
			account.Code = Decrypt(account.Code);
			result.Account = account;
			return result;

			string Decrypt(string value) => string.IsNullOrEmpty(value) ? value : Encoding.UTF8.GetString(this.Authority.Signing.Decrypt(System.Convert.FromBase64String(value)));
		}
		#endregion

	}
}
