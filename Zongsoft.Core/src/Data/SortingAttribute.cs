/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Linq;
using System.Reflection;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据服务对查询操作的排序设置。
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
	public class SortingAttribute : Attribute
	{
		#region 构造函数
		public SortingAttribute(string expression)
		{
			this.Expression = expression ?? throw new ArgumentNullException();
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置排序表达式文本。
		/// </summary>
		public string Expression
		{
			get; set;
		}
		#endregion

		#region 静态方法
		public static Sorting[] GetSortings(string expression)
		{
			if(string.IsNullOrEmpty(expression))
				return Array.Empty<Sorting>();

			return Common.StringExtension.Slice<Sorting>(expression, ',', Sorting.TryParse).ToArray();
		}

		public static Sorting[] GetSortings(Type type)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			if(type == typeof(object) || type.IsValueType)
				return Array.Empty<Sorting>();

			var attribute = type.GetCustomAttribute<SortingAttribute>(true);
			return attribute == null ? Array.Empty<Sorting>() : GetSortings(attribute.Expression);
		}
		#endregion
	}
}
