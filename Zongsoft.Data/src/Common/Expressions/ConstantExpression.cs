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

namespace Zongsoft.Data.Common.Expressions;

public class ConstantExpression : Expression
{
	#region 单例字段
	public static readonly ConstantExpression Null = new();
	#endregion

	#region 构造函数
	private ConstantExpression() { }
	public ConstantExpression(object value)
	{
		this.Value = value ?? throw new ArgumentNullException(nameof(value));

		var valueType = value.GetType();

		if(valueType.IsEnum)
			this.Value = System.Convert.ChangeType(value, Enum.GetUnderlyingType(valueType));
		else if(valueType.IsArray)
		{
			valueType = valueType.GetElementType();

			if(valueType.IsEnum)
			{
				var source = (Array)value;
				var underlying = Enum.GetUnderlyingType(valueType);
				var target = Array.CreateInstance(underlying, source.Length);

				for(var i = 0; i < source.Length; i++)
				{
					target.SetValue(System.Convert.ChangeType(source.GetValue(i), underlying), i);
				}

				this.Value = target;
			}
		}
	}
	#endregion

	#region 公共属性
	public object Value { get; }
	public Type ValueType => this.Value == null ? typeof(object) : this.Value.GetType();
	#endregion
}
