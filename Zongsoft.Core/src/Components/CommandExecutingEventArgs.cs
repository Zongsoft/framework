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
public class CommandExecutingEventArgs : EventArgs
{
	#region 成员变量
	private readonly CommandContext _context;
	private object _value;
	#endregion

	#region 构造函数
	public CommandExecutingEventArgs(CommandContext context, bool cancel = false)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		this.Cancel = cancel;
		this.Parameters = context.Parameters;
	}

	public CommandExecutingEventArgs(object argument, bool cancel = false)
	{
		if(argument is CommandContext context)
		{
			_context = context;
			this.Parameters = context.Parameters;
		}
		else
		{
			_value = argument;
			this.Parameters = new();
		}

		this.Cancel = cancel;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置一个值，表示是否取消当前命令的执行。</summary>
	public bool Cancel { get; set; }

	/// <summary>获取命令的执行上下文对象。</summary>
	public CommandContext Context => _context;

	/// <summary>获取命令的输入值。</summary>
	public object Value => _value ?? _context?.Value;

	/// <summary>获取或设置命令的执行结果。</summary>
	public object Result { get; set; }

	/// <summary>获取命令执行的附加参数集。</summary>
	public Collections.Parameters Parameters { get; set; }
	#endregion
}
