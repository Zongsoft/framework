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

namespace Zongsoft.Security.Privileges;

/// <summary>表示身份验证失败时引发的异常。</summary>
[Serializable]
public class AuthenticationException : SecurityException
{
	#region 构造函数
	public AuthenticationException() : base() { }
	public AuthenticationException(string reason) : base(reason, null, null) { }
	public AuthenticationException(string reason, string message) : base(reason, message, null) { }
	public AuthenticationException(string reason, string message, Exception innerException) : base(reason, message, innerException) { }
	#endregion
}