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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Components;

[Serializable]
public class CommandExecutedEventArgs : EventArgs
{
	#region 成员变量
	private readonly CommandContext _context;
	private object _value;
	#endregion

	#region 构造函数
	public CommandExecutedEventArgs(CommandContext context, object result = null)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		this.Result = result;
		this.Parameters = context.Parameters;
	}

	public CommandExecutedEventArgs(CommandContext context, Exception exception)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		this.Exception = exception;
		this.Parameters = context.Parameters;
	}

	public CommandExecutedEventArgs(object argument, object result = null)
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

		this.Result = result;
	}

	public CommandExecutedEventArgs(object argument, Exception exception)
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

		this.Exception = exception;
	}
	#endregion

	#region 公共属性
	/// <summary>获取命令执行过程中的异常，如果返回空则表示为发生异常。</summary>
	public Exception Exception { get; }

	/// <summary>获取或设置异常是否处理完成，如果返回假(<c>False</c>)则异常信息将被抛出。</summary>
	public bool ExceptionHandled { get; set; }

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
