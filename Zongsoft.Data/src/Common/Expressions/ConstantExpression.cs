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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
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
