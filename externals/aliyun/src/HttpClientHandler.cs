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
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Zongsoft.Externals.Aliyun
{
	internal class HttpClientHandler : System.Net.Http.HttpClientHandler
	{
		#region 成员字段
		private ICertificate _certificate;
		private HttpAuthenticator _authenticator;
		#endregion

		#region 构造函数
		public HttpClientHandler(ICertificate certificate, HttpAuthenticator authenticator)
		{
			_certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
			_authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
		}
		#endregion

		#region 重写方法
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			request.Headers.Date = DateTime.UtcNow;

			switch(_authenticator.SignatureMode)
			{
				case HttpSignatureMode.Header:
					request.Headers.Authorization = new AuthenticationHeaderValue(_authenticator.Name, _certificate.Name + ":" + _authenticator.Signature(request, _certificate.Secret));
					break;
				case HttpSignatureMode.Parameter:
					var delimiter = string.IsNullOrWhiteSpace(request.RequestUri.Query) ? "?" : "&";
					var signature = Uri.EscapeDataString(_authenticator.Signature(request, _certificate.Secret));

					request.RequestUri = new Uri(
						request.RequestUri.Scheme + "://" +
						request.RequestUri.Authority +
						request.RequestUri.PathAndQuery + delimiter +
						_authenticator.Name + "="  + signature +
						request.RequestUri.Fragment);

					break;
			}

			return base.SendAsync(request, cancellationToken);
		}
		#endregion
	}
}
