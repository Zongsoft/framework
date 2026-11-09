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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components;

/// <summary>扩展命令接口。</summary>
public interface ICommand
{
	#region 事件定义
	/// <summary>在 <seealso cref="Enabled"/> 属性发生改变之后发生。</summary>
	event EventHandler EnabledChanged;
	/// <summary>在 <seealso cref="ExecuteAsync"/> 方法执行之前发生。可以通过事件参数取消后续的执行。</summary>
	event EventHandler<CommandExecutedEventArgs> Executed;
	/// <summary>在 <seealso cref="ExecuteAsync"/> 方法执行之后发生。</summary>
	event EventHandler<CommandExecutingEventArgs> Executing;
	#endregion

	#region 属性定义
	/// <summary>获取命令的名称。</summary>
	string Name { get; }
	/// <summary>获取或设置一个值，该值指示命令是否可以执行。</summary>
	bool Enabled { get; set; }
	#endregion

	#region 方法定义
	/// <summary>判断当前命令能否依据给定的选项和参数执行。</summary>
	/// <param name="argument">判断命令能否执行的参数对象。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回一个值，指示是否可以执行。</returns>
	ValueTask<bool> CanExecuteAsync(object argument, CancellationToken cancellation = default);

	/// <summary>执行命令。</summary>
	/// <param name="argument">执行命令的参数对象。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回执行的返回结果。</returns>
	/// <remarks>对实现着的要求：应该在本方法的实现中首先调用<see cref="CanExecuteAsync"/>方法，以确保阻止非法的调用。</remarks>
	ValueTask<object> ExecuteAsync(object argument, CancellationToken cancellation = default);
	#endregion
}
