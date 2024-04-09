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

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据服务的注解类。
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
	public class DataServiceAttribute : Attribute
	{
		#region 构造函数
		public DataServiceAttribute() { }
		public DataServiceAttribute(string sortings) => this.Sortings = sortings;
		public DataServiceAttribute(Type criteria, string sortings = null)
		{
			this.Criteria = criteria;
			this.Sortings = sortings;
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置查询或过滤条件的实体类型。</summary>
		public Type Criteria { get; set; }

		/// <summary>获取或设置排序规则。注：成员之间以逗号分隔。</summary>
		public string Sortings { get; set; }
		#endregion

		#region 公共方法
		public Sorting[] GetSortings()
		{
			if(string.IsNullOrEmpty(this.Sortings))
				return null;

			return Common.StringExtension.Slice<Sorting>(this.Sortings, ',', Sorting.TryParse).ToArray();
		}
		#endregion
	}

	/// <summary>
	/// 表示数据服务的注解类。
	/// </summary>
	/// <typeparam name="TCriteria">数据服务的查询或过滤条件的实体类型。</typeparam>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
	public class DataServiceAttribute<TCriteria> : DataServiceAttribute where TCriteria : class, IModel
	{
		#region 构造函数
		/// <summary>构造一个数据服务注解。</summary>
		/// <param name="sortings">指定的默认排序规则。</param>
		public DataServiceAttribute(params string[] sortings) : base(typeof(TCriteria))
		{
			if(sortings != null && sortings.Length > 0)
				this.Sortings = string.Join(',', sortings);
		}
		#endregion
	}
}
