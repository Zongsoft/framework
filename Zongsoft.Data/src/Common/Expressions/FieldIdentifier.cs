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
using System.Collections.Generic;

namespace Zongsoft.Data.Common.Expressions
{
	/// <summary>
	/// 表示字段标识的表达式。
	/// </summary>
	public class FieldIdentifier : Expression, IIdentifier, IEquatable<FieldIdentifier>
	{
		#region 构造函数
		public FieldIdentifier(ISource table, string name, string alias = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Table = table ?? throw new ArgumentNullException(nameof(table));
			this.Name = name.Trim();
			this.Alias = alias;
		}

		public FieldIdentifier(string name, string alias = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = name.Trim();
			this.Alias = alias;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取字段标识所属的源（表标识、关联子句）。
		/// </summary>
		public ISource Table { get; }

		/// <summary>
		/// 获取字段名称。
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// 获取或设置字段标识的别名。
		/// </summary>
		public string Alias { get; set; }

		/// <summary>
		/// 获取或设置字段标识对应的实体属性标记。
		/// </summary>
		public Metadata.DataEntityPropertyToken Token { get; set; }
		#endregion

		#region 公共方法
		public BinaryExpression Add(IExpression value)
		{
			return Expression.Add(this, value);
		}

		public BinaryExpression Subtract(IExpression value)
		{
			return Expression.Subtract(this, value);
		}

		public BinaryExpression Multiply(IExpression value)
		{
			return Expression.Multiply(this, value);
		}

		public BinaryExpression Divide(IExpression value)
		{
			return Expression.Divide(this, value);
		}

		public BinaryExpression Modulo(IExpression value)
		{
			return Expression.Modulo(this, value);
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			if(string.IsNullOrEmpty(this.Table.Alias))
			{
				if(string.IsNullOrEmpty(this.Alias))
					return this.Name;
				else
					return this.Name + " AS " + this.Alias;
			}
			else
			{
				if(string.IsNullOrEmpty(this.Alias))
					return this.Table.Alias + "." + this.Name;
				else
					return this.Table.Alias + "." + this.Name + " AS " + this.Alias;
			}
		}

		public override int GetHashCode()
		{
			var alias = this.Alias;

			if(string.IsNullOrEmpty(alias))
				return this.Table.GetHashCode() ^ this.Name.ToUpperInvariant().GetHashCode();
			else
				return this.Table.GetHashCode() ^ this.Name.ToUpperInvariant().GetHashCode() ^ alias.ToUpperInvariant().GetHashCode();
		}

		public bool Equals(FieldIdentifier other)
		{
			if(other == null)
				return false;

			return this.Table == other.Table &&
			       string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
			       string.Equals(this.Alias, other.Alias, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj)
		{
			if(obj == null || obj.GetType() != this.GetType())
				return false;

			return base.Equals((FieldIdentifier)obj);
		}
		#endregion
	}
}
