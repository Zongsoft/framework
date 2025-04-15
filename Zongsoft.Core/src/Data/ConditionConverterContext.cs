﻿/*
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

namespace Zongsoft.Data;

/// <summary>
/// 表示条件转换器的上下文类。
/// </summary>
public class ConditionConverterContext
{
	#region 构造函数
	public ConditionConverterContext(IModel criteria, ConditionBehaviors behaviors, string path, string[] names, Type type, object value, ConditionOperator? @operator = null)
	{
		this.Criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));
		this.Names = names ?? throw new ArgumentNullException(nameof(names));
		this.Type = type ?? throw new ArgumentNullException(nameof(type));
		this.Path = path;
		this.Value = value;
		this.Operator = @operator;
		this.Behaviors = behaviors;
	}
	#endregion

	#region 公共属性
	/// <summary>获取转换的条件成员所在的条件对象。</summary>
	public IModel Criteria { get; }

	/// <summary>获取条件转换的行为模式。</summary>
	public ConditionBehaviors Behaviors { get; }

	/// <summary>获取转换的字段名数组。</summary>
	public string[] Names { get; }

	/// <summary>获取转换成员的层级路径。</summary>
	public string Path { get; }

	/// <summary>获取转换的成员类型。</summary>
	public Type Type { get; }

	/// <summary>获取转换的成员值。</summary>
	public object Value { get; }

	/// <summary>获取转换的条件操作符。</summary>
	public ConditionOperator? Operator { get; }
	#endregion

	#region 公共方法
	public string GetFullName(string name = null)
	{
		if(string.IsNullOrEmpty(name))
			name = this.Names[0];

		return string.IsNullOrEmpty(this.Path) ? name : this.Path + '.' + name;
	}

	public string GetFullName(int index)
	{
		var names = this.Names;

		if(index < 0 || index >= names.Length)
			throw new ArgumentOutOfRangeException(nameof(index));

		return string.IsNullOrEmpty(this.Path) ? names[index] : this.Path + '.' + names[index];
	}
	#endregion
}
