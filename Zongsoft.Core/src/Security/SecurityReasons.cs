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

namespace Zongsoft.Security;

/// <summary>提供安全理由短语的定义类。</summary>
public static class SecurityReasons
{
	/// <summary>未知的原因</summary>
	public static readonly string Unknown = nameof(Unknown);

	/// <summary>禁止验证通过</summary>
	public static readonly string Forbidden = nameof(Forbidden);

	/// <summary>校验失败</summary>
	public static readonly string VerifyFaild = nameof(VerifyFaild);

	/// <summary>无效的身份标识</summary>
	public static readonly string InvalidIdentity = nameof(InvalidIdentity);

	/// <summary>无效的密码</summary>
	public static readonly string InvalidPassword = nameof(InvalidPassword);

	/// <summary>无效的参数。</summary>
	public static readonly string InvalidArgument = nameof(InvalidArgument);

	/// <summary>帐户尚未批准</summary>
	public static readonly string AccountUnapproved = nameof(AccountUnapproved);

	/// <summary>帐户被暂时挂起（可能因为密码验证失败次数过多）</summary>
	public static readonly string AccountSuspended = nameof(AccountSuspended);

	/// <summary>帐户已被禁用</summary>
	public static readonly string AccountDisabled = nameof(AccountDisabled);
}
