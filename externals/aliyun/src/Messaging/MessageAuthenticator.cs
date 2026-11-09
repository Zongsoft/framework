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

namespace Zongsoft.Externals.Aliyun.Messaging;

internal class MessageAuthenticator : HttpAuthenticator
{
	#region 单例字段
	public static MessageAuthenticator Instance = new MessageAuthenticator("MNS");
	#endregion

	#region 私有构造
	private MessageAuthenticator(string name) : base(name, HttpSignatureMode.Header) { }
	#endregion

	#region 重写方法
	protected override bool IsCanonicalizedHeader(string name)
	{
		return name.StartsWith("x-mns-");
	}

	protected override string CanonicalizeResource(HttpRequestMessage request)
	{
		return request.RequestUri.PathAndQuery;
	}
	#endregion
}
