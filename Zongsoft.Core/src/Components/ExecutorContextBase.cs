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
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.Components;

public abstract class ExecutorContextBase : IExecutorContext
{
	#region 成员字段
	private Exception _exception;
	#endregion

	#region 构造函数
	/// <summary>创建具有空参数集合的执行上下文。</summary>
	/// <param name="executor">本次调用的执行器。</param>
	protected ExecutorContextBase(IExecutor executor)
	{
		this.Executor = executor;
		this.Features = Array.Empty<IFeature>();
		this.Parameters = new Collections.Parameters();
	}

	/// <summary>创建使用指定共享参数的执行上下文。</summary>
	/// <param name="executor">本次调用的执行器。</param>
	/// <param name="parameters">共享参数；<see langword="null"/> 时创建空集合。</param>
	protected ExecutorContextBase(IExecutor executor, Collections.Parameters parameters)
	{
		this.Executor = executor;
		this.Features = Array.Empty<IFeature>();
		this.Parameters = parameters ?? new Collections.Parameters();
	}

	/// <summary>创建执行上下文，并将指定键值对复制到参数集合。</summary>
	/// <param name="executor">本次调用的执行器。</param>
	/// <param name="parameters">用于初始化参数集合的键值对。</param>
	protected ExecutorContextBase(IExecutor executor, IEnumerable<KeyValuePair<string, object>> parameters)
	{
		this.Executor = executor;
		this.Features = Array.Empty<IFeature>();
		this.Parameters = new Collections.Parameters(parameters);
	}

	/// <summary>创建携带运行时特性快照的执行上下文。</summary>
	/// <param name="executor">本次调用的执行器。</param>
	/// <param name="parameters">共享参数；<see langword="null"/> 时创建空集合。</param>
	/// <param name="features">运行时特性；立即复制集合，保留各特性的引用身份。</param>
	protected ExecutorContextBase(IExecutor executor, Collections.Parameters parameters, IEnumerable<IFeature> features) : this(executor, parameters)
	{
		this.Features = features == null ? Array.Empty<IFeature>() : Array.AsReadOnly(features.ToArray());
	}
	#endregion

	#region 公共属性
	/// <summary>获取处理本次执行请求的执行器。</summary>
	public IExecutor Executor { get; }
	/// <summary>获取本次执行过程中共享的参数集合。</summary>
	public Collections.Parameters Parameters { get; }
	/// <summary>获取本次执行的运行时特性快照。</summary>
	public IReadOnlyList<IFeature> Features { get; }
	#endregion

	#region 显式实现
	/// <summary>获取本次执行的实参。</summary>
	/// <returns>当前执行请求的实参。</returns>
	protected abstract object GetArgument();
	/// <summary>获取本次执行的实参。</summary>
	object IExecutorContext.Argument => this.GetArgument();
	#endregion

	#region 公共方法
	/// <summary>记录本次执行的错误。</summary>
	/// <param name="exception">执行错误；<see langword="null"/> 表示清除已有错误。</param>
	public void Error(Exception exception) => _exception = exception;
	/// <summary>检查本次执行是否存在错误。</summary>
	/// <param name="exception">已记录的执行错误；不存在时为 <see langword="null"/>。</param>
	/// <returns>存在错误时为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
	public bool HasError(out Exception exception) => (exception = _exception) != null;
	#endregion
}
