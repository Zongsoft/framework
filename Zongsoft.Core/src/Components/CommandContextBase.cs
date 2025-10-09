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
using System.IO;
using System.Collections.Generic;

namespace Zongsoft.Components;

/// <summary>
/// 表示命令执行器的上下文（命令执行会话）类。
/// </summary>
public abstract class CommandContextBase
{
	#region 成员字段
	private readonly CommandContextBase _context;
	private readonly ICommandExecutor _executor;
	private readonly CommandExpression _expression;
	private readonly Collections.Parameters _parameters;
	private object _value;
	private object _result;
	#endregion

	#region 构造函数
	protected CommandContextBase(ICommandExecutor executor, CommandExpression expression, object value = null)
	{
		_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		_expression = expression ?? throw new ArgumentNullException(nameof(expression));
		_value = value;
		_parameters = new();
	}

	protected CommandContextBase(CommandContextBase context, object value = null) : this(context, null, value) { }
	protected CommandContextBase(CommandContextBase context, CommandExpression expression, object value = null)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_parameters = context.Parameters;
		_executor = context.Executor;
		_expression = expression ?? context.Expression;
		_value = value ?? context.Value;
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前命令执行器对象。</summary>
	public ICommandExecutor Executor => _executor;

	/// <summary>获取当前命令执行器的命令表达式。</summary>
	public CommandExpression Expression => _expression;

	/// <summary>获取当前命令描述信息。</summary>
	public CommandDescriptor Descriptor { get; }

	/// <summary>获取当前命令的参数数组。</summary>
	public string[] Arguments { get; }

	/// <summary>获取从命令执行器传入的值。</summary>
	public object Value => _value ?? _context?.Value;

	/// <summary>获取或设置命令执行器的最终结果。</summary>
	public object Result
	{
		get => _result ?? _context?.Result;
		set => _result = value;
	}

	/// <summary>获取当前命令执行器的标准输出器。</summary>
	public ICommandOutlet Output => this.Executor.Output;

	/// <summary>获取当前命令执行器的错误输出器。</summary>
	public TextWriter Error => this.Executor.Error;

	/// <summary>获取当前命令会话的共享参数集。</summary>
	public Collections.Parameters Parameters => _parameters;
	#endregion

	#region 公共方法
	public CommandLine.CmdletOptionCollection GetOptions(IEnumerable<CommandLine.CmdletOption> options)
	{
		return CommandLine.GetOptions(this.Descriptor, options);
	}

	public TOptions GetOptions<TOptions>(IEnumerable<CommandLine.CmdletOption> options)
	{
		return CommandLine.GetOptions<TOptions>(this.Descriptor, options);
	}
	#endregion
}
