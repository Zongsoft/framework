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

namespace Zongsoft.Security.Configuration;

/// <summary>表示身份验证方式的枚举。</summary>
[Flags]
public enum IdentityVerification
{
	/// <summary>无验证。</summary>
	None,

	/// <summary>验证电子邮箱的有效性。</summary>
	Email,

	/// <summary>验证手机号码的有效性。</summary>
	Phone,
}
