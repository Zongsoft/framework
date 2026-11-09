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
