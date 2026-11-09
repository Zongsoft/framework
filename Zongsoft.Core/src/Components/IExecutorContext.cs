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

/// <summary>表示执行器上下文的接口。</summary>
public interface IExecutorContext
{
	/// <summary>获取处理本次执行请求的执行器。</summary>
	IExecutor Executor { get; }

	/// <summary>获取执行请求对象。</summary>
	object Argument { get; }

	/// <summary>获取可用于在本次执行过程中在各处理模块之间组织和共享数据的键/值集合。</summary>
	Collections.Parameters Parameters { get; }

	/// <summary>获取本次执行绑定的运行时特性快照；不作为管线策略配置。</summary>
	System.Collections.Generic.IReadOnlyList<IFeature> Features { get; }

	/// <summary>设置一个异常。</summary>
	/// <param name="exception">发生的异常对象。</param>
	void Error(Exception exception);

	/// <summary>获取一个值，指示本次执行中是否发生了异常。</summary>
	/// <param name="exception">输出参数，不为空则表示发生的异常。</param>
	/// <returns>返回真(<c>True</c>)表示执行过程有错误，否则返回假(<c>False</c>)。</returns>
	bool HasError(out Exception exception);
}
