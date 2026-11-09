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
using System.Net.Http;
using System.Net.Http.Headers;

namespace Zongsoft.Externals.Aliyun;

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
				request.Headers.Authorization = new AuthenticationHeaderValue(_authenticator.Name, _certificate.Code + ":" + _authenticator.Signature(request, _certificate.Secret));
				break;
			case HttpSignatureMode.Parameter:
				var delimiter = string.IsNullOrWhiteSpace(request.RequestUri.Query) ? "?" : "&";
				var signature = Uri.EscapeDataString(_authenticator.Signature(request, _certificate.Secret));

				request.RequestUri = new Uri(
					request.RequestUri.Scheme + "://" +
					request.RequestUri.Authority +
					request.RequestUri.PathAndQuery + delimiter +
					_authenticator.Name + "=" + signature +
					request.RequestUri.Fragment);

				break;
		}

		return base.SendAsync(request, cancellationToken);
	}
	#endregion
}
