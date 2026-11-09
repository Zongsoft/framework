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

/// <summary>表示用户管理的配置选项。</summary>
public class IdentityOptions
{
	/// <summary>获取或设置密码的最小长度，零表示不限制。</summary>
	public int PasswordLength { get; set; }

	/// <summary>获取或设置密码的强度。</summary>
	public PasswordStrength PasswordStrength { get; set; }

	/// <summary>获取或设置用户信息的有效性校验项。</summary>
	public IdentityVerification Verification { get; set; }
}
