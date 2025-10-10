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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Components;

/// <summary>
/// 表示命令执行器的上下文（命令执行会话）类。
/// </summary>
public abstract class CommandContextBase : ICommandContext
{
	#region 成员字段
	private readonly ICommandContext _context;
	private readonly ICommandExecutor _executor;
	private readonly Collections.Parameters _parameters;
	private readonly CommandLine.Cmdlet _cmdlet;
	private CommandLine.CmdletOptionCollection _options;
	private object _value;
	private object _result;
	#endregion

	#region 构造函数
	protected CommandContextBase(ICommandExecutor executor, CommandLine.Cmdlet cmdlet, object value)
	{
		_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		_value = value;
		_parameters = new();
		_cmdlet = cmdlet ?? throw new ArgumentNullException(nameof(cmdlet));
		this.Arguments = new(cmdlet.Arguments);
	}

	protected CommandContextBase(ICommandContext context, CommandLine.Cmdlet cmdlet, object value)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_parameters = context.Parameters;
		_executor = context.Executor;
		_value = value ?? context.Value;
		_cmdlet = cmdlet ?? throw new ArgumentNullException(nameof(cmdlet));
		this.Arguments = new(cmdlet.Arguments);
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前命令执行器对象。</summary>
	public ICommandExecutor Executor => _executor;

	/// <summary>获取当前命令描述信息。</summary>
	public virtual CommandDescriptor Descriptor { get; }

	/// <summary>获取当前命令的参数数组。</summary>
	public CommandArgumentCollection Arguments { get; }

	/// <summary>获取或设置传入的值。</summary>
	public object Value { get => _value ?? _context?.Value; set => _value = value; }

	/// <summary>获取或设置执行结果。</summary>
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
	public TOptions GetOptions<TOptions>() => CommandLine.GetOptions<TOptions>(this.Descriptor, _cmdlet.Options);
	public CommandLine.CmdletOptionCollection GetOptions() => _options ??= CommandLine.GetOptions(this.Descriptor, _cmdlet.Options);
	#endregion
}
