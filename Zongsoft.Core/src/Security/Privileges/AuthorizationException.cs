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

public class AuthorizationException : SecurityException
{
	#region 构造函数
	public AuthorizationException() : base(SecurityReasons.Forbidden, Properties.Resources.AuthorizationException_Message) { }
	public AuthorizationException(string reason) : base(reason, Properties.Resources.AuthorizationException_Message) { }
	public AuthorizationException(string reason, string message) : base(reason, message ?? Properties.Resources.AuthorizationException_Message) { }
	public AuthorizationException(string reason, string message, Exception innerException) : base(reason, message ?? Properties.Resources.AuthorizationException_Message, innerException) { }
	#endregion
}
