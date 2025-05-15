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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components;

/// <summary>
/// 提供实现<see cref="ICommand"/>接口功能的基类，建议需要完成<see cref="ICommand"/>接口功能的实现者从此类继承。
/// </summary>
/// <typeparam name="TContext">指定命令的执行上下文类型。</typeparam>
public abstract class CommandBase<TContext> : CommandBase, ICommand<TContext> where TContext : CommandContext
{
	#region 构造函数
	protected CommandBase() : base(null, true) { }
	protected CommandBase(string name) : base(name, true) { }
	protected CommandBase(string name, bool enabled) : base(name, enabled) { }
	#endregion

	#region 公共方法
	/// <summary>判断命令是否可被执行。</summary>
	/// <param name="context">判断命令是否可被执行的上下文对象。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果返回真(true)则表示命令可被执行，否则表示不可执行。</returns>
	/// <remarks>
	///		<para>本方法为虚拟方法，可由子类更改基类的默认实现方式。</para>
	///		<para>如果<seealso cref="CommandBase.Predication"/>属性为空(null)，则返回<seealso cref="CommandBase.Enabled"/>属性值；否则返回由<seealso cref="CommandBase.Predication"/>属性指定的断言对象的断言方法的值。</para>
	/// </remarks>
	public virtual ValueTask<bool> CanExecuteAsync(TContext context, CancellationToken cancellation = default) => base.CanExecuteAsync(context, cancellation);

	/// <summary>执行命令。</summary>
	/// <param name="context">执行命令的上下文对象。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回执行的返回结果。</returns>
	/// <remarks>
	///		<para>本方法的实现中首先调用<seealso cref="CommandBase.CanExecuteAsync"/>方法，以确保阻止非法的调用。</para>
	/// </remarks>
	public ValueTask<object> ExecuteAsync(TContext context, CancellationToken cancellation = default) => base.ExecuteAsync(context, cancellation);
	#endregion

	#region 抽象方法
	protected virtual TContext CreateContext(object argument) => argument as TContext;
	protected abstract ValueTask<object> OnExecuteAsync(TContext context, CancellationToken cancellation);
	#endregion

	#region 重写方法
	protected override ValueTask<bool> CanExecuteAsync(object argument, CancellationToken cancellation)
	{
		if(argument is not TContext context)
			context = this.CreateContext(argument);

		return this.CanExecuteAsync(context, cancellation);
	}

	protected override ValueTask<object> OnExecuteAsync(object argument, CancellationToken cancellation)
	{
		if(argument is not TContext context)
			context = this.CreateContext(argument);

		//执行具体的命令操作
		return this.OnExecuteAsync(context, cancellation);
	}
	#endregion
}
