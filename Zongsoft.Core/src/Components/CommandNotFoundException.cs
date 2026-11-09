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

public class CommandNotFoundException : CommandException
{
	#region 构造函数
	public CommandNotFoundException(string path) => this.Path = path ?? string.Empty;
	#endregion

	#region 公共属性
	public string Path { get; }
	#endregion

	#region 重写属性
	public override string Message => string.Format(Properties.Resources.CommandNotFound_Message, this.Path);
	#endregion
}
