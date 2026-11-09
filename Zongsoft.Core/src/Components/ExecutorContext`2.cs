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
using System.Collections.Generic;

namespace Zongsoft.Components;

public class ExecutorContext<TArgument, TResult> : ExecutorContextBase, IExecutorContext<TArgument, TResult>
{
	#region 构造函数
	/// <summary>创建包含实参及共享参数的执行上下文。</summary>
	/// <param name="executor">本次调用的执行器。</param>
	/// <param name="argument">执行实参。</param>
	/// <param name="parameters">共享参数；<see langword="null"/> 时创建空集合。</param>
	public ExecutorContext(IExecutor executor, TArgument argument, Collections.Parameters parameters = null) : base(executor, parameters)
	{
		this.Argument = argument;
	}

	/// <summary>创建包含初始结果、实参及共享参数的执行上下文。</summary>
	/// <param name="executor">本次调用的执行器。</param>
	/// <param name="result">初始执行结果。</param>
	/// <param name="argument">执行实参。</param>
	/// <param name="parameters">共享参数；<see langword="null"/> 时创建空集合。</param>
	public ExecutorContext(IExecutor executor, TResult result, TArgument argument, Collections.Parameters parameters = null) : base(executor, parameters)
	{
		this.Result = result;
		this.Argument = argument;
	}

	/// <summary>创建包含实参的执行上下文，并复制指定参数。</summary>
	/// <param name="executor">本次调用的执行器。</param>
	/// <param name="argument">执行实参。</param>
	/// <param name="parameters">用于初始化参数集合的键值对。</param>
	public ExecutorContext(IExecutor executor, TArgument argument, IEnumerable<KeyValuePair<string, object>> parameters = null) : base(executor, parameters)
	{
		this.Argument = argument;
	}

	/// <summary>创建包含初始结果及实参的执行上下文，并复制指定参数。</summary>
	/// <param name="executor">本次调用的执行器。</param>
	/// <param name="result">初始执行结果。</param>
	/// <param name="argument">执行实参。</param>
	/// <param name="parameters">用于初始化参数集合的键值对。</param>
	public ExecutorContext(IExecutor executor, TResult result, TArgument argument, IEnumerable<KeyValuePair<string, object>> parameters = null) : base(executor, parameters)
	{
		this.Result = result;
		this.Argument = argument;
	}

	/// <summary>创建携带运行时特性快照的执行上下文。</summary>
	/// <param name="executor">本次调用的执行器。</param>
	/// <param name="argument">执行实参。</param>
	/// <param name="parameters">共享参数；<see langword="null"/> 时创建空集合。</param>
	/// <param name="features">立即复制的运行时特性集合；保留特性的引用身份。</param>
	public ExecutorContext(IExecutor executor, TArgument argument, Collections.Parameters parameters, IEnumerable<IFeature> features) : base(executor, parameters, features)
	{
		this.Argument = argument;
	}
	#endregion

	#region 公共属性
	public TArgument Argument { get; }
	public TResult Result { get; set; }
	#endregion

	#region 重写方法
	protected override object GetArgument() => this.Argument;
	#endregion
}
