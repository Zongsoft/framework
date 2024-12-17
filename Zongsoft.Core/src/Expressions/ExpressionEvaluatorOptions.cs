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
using System.IO;
using System.Collections.Generic;

using Zongsoft.Collections;

namespace Zongsoft.Expressions;

public class ExpressionEvaluatorOptions : IExpressionEvaluatorOptions
{
	#region 成员字段
	private TextWriter _error = Console.Error;
	private TextReader _input = Console.In;
	private TextWriter _output = Console.Out;
	#endregion

	#region 构造函数
	public ExpressionEvaluatorOptions() => this.Properties = new Parameters();
	public ExpressionEvaluatorOptions(IEnumerable<KeyValuePair<string, object>> properties) => this.Properties = new Parameters(properties);
	public ExpressionEvaluatorOptions(IEnumerable<KeyValuePair<object, object>> properties) => this.Properties = new Parameters(properties);
	#endregion

	#region 公共属性
	public TextWriter Error { get => _error; set => _error = value ?? throw new ArgumentNullException(nameof(value)); }
	public TextReader Input { get => _input; set => _input = value ?? throw new ArgumentNullException(nameof(value)); }
	public TextWriter Output { get => _output; set => _output = value ?? throw new ArgumentNullException(nameof(value)); }

	public Parameters Properties { get; }
	#endregion

	#region 静态方法
	public static ExpressionEvaluatorOptions In(TextReader input) => new() { Input = input };
	public static ExpressionEvaluatorOptions In(TextReader input, IEnumerable<KeyValuePair<string, object>> properties) => new(properties) { Input = input };
	public static ExpressionEvaluatorOptions In(TextReader input, IEnumerable<KeyValuePair<object, object>> properties) => new(properties) { Input = input };

	public static ExpressionEvaluatorOptions Out(TextWriter output) => new() { Output = output };
	public static ExpressionEvaluatorOptions Out(TextWriter output, IEnumerable<KeyValuePair<string, object>> properties) => new(properties) { Output = output };
	public static ExpressionEvaluatorOptions Out(TextWriter output, IEnumerable<KeyValuePair<object, object>> properties) => new(properties) { Output = output };
	#endregion
}
