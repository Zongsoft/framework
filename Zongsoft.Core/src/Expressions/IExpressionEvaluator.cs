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

namespace Zongsoft.Expressions;

/// <summary>表示表达式运算器的接口。</summary>
public interface IExpressionEvaluator : IDisposable
{
	/// <summary>获取运算器的名称。</summary>
	string Name { get; }
	/// <summary>获取一个值，指示是否已被释放。</summary>
	bool IsDisposed { get; }
	/// <summary>获取全局变量集合。</summary>
	IDictionary<string, object> Global { get; }
	/// <summary>获取或设置全局默认配置。</summary>
	IExpressionEvaluatorOptions Options { get; set; }

	/// <summary>运算表达式。</summary>
	/// <param name="expression">指定的表达式文本。</param>
	/// <param name="variables">指定的环境变量集。</param>
	/// <returns>返回的表达式结果。</returns>
	object Evaluate(string expression, IDictionary<string, object> variables = null);

	/// <summary>运算表达式。</summary>
	/// <param name="expression">指定的表达式文本。</param>
	/// <param name="options">指定的运算选项。</param>
	/// <param name="variables">指定的环境变量集。</param>
	/// <returns>返回的表达式结果。</returns>
	object Evaluate(string expression, IExpressionEvaluatorOptions options, IDictionary<string, object> variables = null);
}