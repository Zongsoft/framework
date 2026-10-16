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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components;

/// <summary>表示处理程序的基类。</summary>
/// <typeparam name="TArgument">处理程序的请求参数类型。</typeparam>
public abstract class HandlerBase<TArgument> : IHandler<TArgument>, IHandler
{
	#region 构造函数
	protected HandlerBase() { }
	#endregion

	#region 公共方法
	/// <summary>派发实参，附加参数为 <see langword="null" />，不创建空容器。</summary>
	/// <param name="argument">待派发的处理实参。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>处理操作，调用方必须恰好消费一次。</returns>
	public ValueTask HandleAsync(TArgument argument, CancellationToken cancellation = default) => this.OnHandleAsync(argument, null, cancellation);
	/// <summary>原样转发实参与附加参数，保留参数对象身份。</summary>
	/// <param name="argument">待派发的处理实参。</param>
	/// <param name="parameters">附加参数；<see langword="null" /> 原样传递，不创建空容器。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>处理操作。</returns>
	public ValueTask HandleAsync(TArgument argument, Collections.Parameters parameters, CancellationToken cancellation = default)
	{
		return this.OnHandleAsync(argument, parameters, cancellation);
	}
	#endregion

	#region 抽象方法
	/// <summary>处理实参；实现必须允许附加参数为 <see langword="null" />。</summary>
	/// <param name="argument">待派发的处理实参。</param>
	/// <param name="parameters">附加参数；<see langword="null" /> 原样传递，不创建空容器。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>处理操作。</returns>
	protected abstract ValueTask OnHandleAsync(TArgument argument, Collections.Parameters parameters, CancellationToken cancellation);
	#endregion

	#region 显式实现
	/// <summary>转换对象实参并通过无附加参数入口派发。</summary>
	/// <param name="argument">待派发的处理实参。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>处理操作。</returns>
	ValueTask IHandler.HandleAsync(object argument, CancellationToken cancellation) => this.HandleAsync(this.Convert(argument), null, cancellation);
	/// <summary>转换对象实参并原样传递附加参数。</summary>
	/// <param name="argument">待派发的处理实参。</param>
	/// <param name="parameters">附加参数；<see langword="null" /> 原样传递，不创建空容器。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>处理操作。</returns>
	ValueTask IHandler.HandleAsync(object argument, Collections.Parameters parameters, CancellationToken cancellation) => this.HandleAsync(this.Convert(argument), parameters, cancellation);
	#endregion

	#region 参数转换
	/// <summary>将非泛型调用的实参转换为处理器要求的类型。</summary>
	/// <param name="argument">待派发的处理实参。</param>
	/// <returns>转换结果；空引用返回目标类型默认值。</returns>
	protected virtual TArgument Convert(object argument)
	{
		if(argument == null)
			return default;

		return Common.Convert.TryConvertValue<TArgument>(argument, out var value) ? value : throw new ArgumentException(string.Format(Properties.Resources.Conversion_ArgumentType_Message, typeof(TArgument).FullName), nameof(argument));
	}
	#endregion
}
