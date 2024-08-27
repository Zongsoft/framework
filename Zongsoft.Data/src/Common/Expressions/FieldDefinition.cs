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
using System.Collections.ObjectModel;

namespace Zongsoft.Data.Common.Expressions
{
	public class FieldDefinition : Expression
	{
		#region 构造函数
		public FieldDefinition(string name, System.Data.DbType type, bool nullable = true)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = name;
			this.DbType = type;
			this.Nullable = nullable;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取字段的名称。
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// 获取或设置字段的数据类型。
		/// </summary>
		public System.Data.DbType DbType { get; set; }

		/// <summary>
		/// 获取或设置字段是否允许为空。
		/// </summary>
		public bool Nullable { get; set; }

		/// <summary>
		/// 获取或设置数值字段的最大长度。
		/// </summary>
		public int Length { get; set; }

		/// <summary>
		/// 获取或设置数值字段的精度。
		/// </summary>
		public byte Precision { get; set; }

		/// <summary>
		/// 获取或设置数值字段的小数点位数。
		/// </summary>
		public byte Scale { get; set; }
		#endregion

		#region 重写方法
		public override string ToString()
		{
			return $"{this.Name} {this.DbType} {(this.Nullable ? "NULL" : "NOT NULL")}";
		}
		#endregion
	}

	public class FieldDefinitionCollection() : KeyedCollection<string, FieldDefinition>(StringComparer.OrdinalIgnoreCase)
	{
		protected override string GetKeyForItem(FieldDefinition field) => field.Name;
	}
}
