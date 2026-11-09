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
using System.Net.Http;

namespace Zongsoft.Externals.Aliyun.Telecom;

public class PhoneAuthenticator : HttpAuthenticator
{
	#region 单例字段
	public static readonly PhoneAuthenticator Instance = new();
	#endregion

	#region 私有构造
	private PhoneAuthenticator() : base("Signature", HttpSignatureMode.Parameter)
	{
	}
	#endregion

	#region 重写方法
	public override string Signature(HttpRequestMessage request, string secret)
	{
		return base.Signature(request, secret + "&");
	}

	protected override string Canonicalize(HttpRequestMessage request)
	{
		var canonicalizedString = base.Canonicalize(request);

		return request.Method.Method + "&%2F&" + Uri.EscapeDataString(canonicalizedString);
	}

	protected override string CanonicalizeHeaders(HttpRequestMessage request)
	{
		return null;
	}

	protected override string CanonicalizeResource(HttpRequestMessage request)
	{
		return CanonicalizeQuery(request.RequestUri, tx => tx.Replace("%7E", "~"));
	}
	#endregion
}
