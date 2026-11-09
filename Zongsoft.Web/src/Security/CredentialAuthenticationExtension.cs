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

using Microsoft.AspNetCore.Authentication;

namespace Zongsoft.Web.Security;

public static class CredentialAuthenticationExtension
{
	#region 常量定义
	private const string SCHEME = "Credential";
	#endregion

	#region 公共方法
	public static AuthenticationBuilder AddCredentials(this AuthenticationBuilder builder, Action<CredentialAuthenticationOptions> configure = null)
	{
		builder.AddScheme<CredentialAuthenticationOptions, CredentialAuthenticationHandler>(SCHEME, "Zongsoft.Credentials", options => configure?.Invoke(options));
		return builder;
	}
	#endregion
}
