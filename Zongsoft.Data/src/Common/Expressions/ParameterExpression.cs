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
using System.Data;

namespace Zongsoft.Data.Common.Expressions;

public class ParameterExpression : Expression
{
	#region 常量定义
	internal const string Anonymous = "?";
	#endregion

	#region 成员字段
	private object _value;
	private bool _hasValue;
	#endregion

	#region 构造函数
	public ParameterExpression(string name, DataType type, ParameterDirection direction = ParameterDirection.Input)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Type = type;
		this.Direction = direction;
	}

	public ParameterExpression(string name, DataType type, object value)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Direction = ParameterDirection.Input;

		if(value != null)
			this.Value = value;

		this.Type = type;
	}

	public ParameterExpression(FieldIdentifier field, object value) : this(field, null, value) { }
	public ParameterExpression(FieldIdentifier field, SchemaMember schema)
	{
		this.Name = Anonymous;
		this.Schema = schema;
		this.Field = field ?? throw new ArgumentNullException(nameof(field));
		this.Direction = ParameterDirection.Input;

		if(field.Token.Property.IsSimplex)
			this.Type = ((Metadata.IDataEntitySimplexProperty)field.Token.Property).Type;
	}

	public ParameterExpression(FieldIdentifier field, SchemaMember schema, object value)
	{
		this.Name = Anonymous;
		this.Schema = schema;
		this.Field = field ?? throw new ArgumentNullException(nameof(field));
		this.Direction = ParameterDirection.Input;

		/*
		 * 注意：更新Value属性会导致 HasValue 属性值为真！
		 * 而 HasValue 为真的参数可能会在写入操作的参数绑定中被忽略。
		 */
		this.Value = value;

		if(field.Token.Property.IsSimplex)
			this.Type = ((Metadata.IDataEntitySimplexProperty)field.Token.Property).Type;
	}
	#endregion

	#region 公共属性
	/// <summary>获取参数名称。</summary>
	/// <remarks>如果参数名为空或问号(?)表示该参数名由集合定义，当该参数被加入到语句的参数集中，该名称将被更改为特定序号的名字。可参考<see cref="StatementBase.Parameters"/>属性的集合。</remarks>
	public string Name { get; set; }

	/// <summary>获取参数对应的字段标识，可能为空。</summary>
	public FieldIdentifier Field { get; }

	/// <summary>获取或设置参数对应的模式成员，可能为空。</summary>
	public SchemaMember Schema { get; set; }

	/// <summary>获取参数的方向。</summary>
	public ParameterDirection Direction { get; }

	/// <summary>获取或设置参数的数据类型。</summary>
	public DataType Type { get; set; }

	/// <summary>获取或设置参数值。</summary>
	/// <remarks>注意：设置该属性值会导致<see cref="IsChanged"/>属性为真。</remarks>
	public object Value
	{
		get => _value;
		set
		{
			_value = value;

			if(value != null)
			{
				if(value.GetType().IsEnum)
					_value = System.Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()));

				this.Type = Utility.GetDbType(_value).AsDataType();
			}

			_hasValue = true;
		}
	}

	/// <summary>获取一个值，指示 <see cref="Value"/> 属性是否被设置过。</summary>
	public bool IsChanged => _hasValue;
	#endregion

	#region 重写方法
	public override string ToString() => this.IsChanged ?
		$"[{this.Direction}]{this.Name} {this.Type} = {this.Value ?? "<NULL>"}" :
		$"[{this.Direction}]{this.Name} {this.Type} @ {this.Schema}";
	#endregion
}
