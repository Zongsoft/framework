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
public class CommandExecutorFailureEventArgs : Zongsoft.Diagnostics.FailureEventArgs
{
	#region 构造函数
	public CommandExecutorFailureEventArgs(CommandExecutorContext context, Exception exception) : base(exception, false) => this.Context = context ?? throw new ArgumentNullException(nameof(context));
	public CommandExecutorFailureEventArgs(CommandExecutorContext context, Exception exception, bool handled) : base(exception, handled) => this.Context = context ?? throw new ArgumentNullException(nameof(context));
	#endregion

	#region 公共属性
	public CommandExecutorContext Context { get; }
	#endregion
}
