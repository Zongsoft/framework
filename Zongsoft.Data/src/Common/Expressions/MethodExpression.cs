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
using System.Data;
using System.Collections.Generic;

namespace Zongsoft.Data.Common.Expressions;

public class MethodExpression : Expression
{
	#region 构造函数
	protected MethodExpression(string name, MethodType type, IList<IExpression> arguments = null)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name.Trim();
		this.MethodType = type;
		this.Arguments = arguments;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Alias { get; set; }
	public MethodType MethodType { get; }
	public IList<IExpression> Arguments { get; }
	public bool HasArguments => this.Arguments != null && this.Arguments.Count > 0;
	#endregion

	#region 静态方法
	public static MethodExpression Function(string name, params IExpression[] arguments) => new MethodExpression(name, MethodType.Function, arguments);
	public static MethodExpression Procedure(string name, params IExpression[] arguments) => new MethodExpression(name, MethodType.Procedure, arguments);
	#endregion
}

public class CastFunctionExpression : MethodExpression
{
	public CastFunctionExpression(IExpression value, DataType type, int length = 0, string style = null) : base("CAST", MethodType.Function)
	{
		this.Value = value;
		this.Type = type;
		this.Length = length;
		this.Style = style;
	}

	public CastFunctionExpression(IExpression value, DataType type, byte precision, byte scale, string style = null) : base("CAST", MethodType.Function)
	{
		this.Value = value;
		this.Type = type;
		this.Precision = precision;
		this.Scale = scale;
		this.Style = style;
	}

	#region 公共属性
	public IExpression Value { get; }
	public DataType Type { get; }
	public int Length { get; }
	public byte Precision { get; }
	public byte Scale { get; }
	public string Style { get; }
	#endregion
}
