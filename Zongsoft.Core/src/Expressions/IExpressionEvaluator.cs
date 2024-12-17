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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

namespace Zongsoft.Expressions
{
	/// <summary>
	/// 表示表达式运算器的接口。
	/// </summary>
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
}