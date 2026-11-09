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

namespace Zongsoft.Components;

[Serializable]
public class CommandException : ApplicationException
{
	#region 构造函数
	public CommandException() => this.Code = 0;
	public CommandException(string message) : this(0, message, null) { }
	public CommandException(string message, Exception innerException) : this(0, message, innerException) { }
	public CommandException(int code, string message) : this(code, message, null) { }
	public CommandException(int code, string message, Exception innerException) : base(message, innerException)
	{
		this.Code = code;
		this.HasMessage = !string.IsNullOrEmpty(message);
	}
	#endregion

	#region 保护属性
	protected bool HasMessage { get; }
	#endregion

	#region 公共属性
	public int Code { get; }
	#endregion
}
