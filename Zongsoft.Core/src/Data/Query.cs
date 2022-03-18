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

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示查询定义的类。
	/// </summary>
	public class Query : IQuery
	{
		#region 构造函数
		public Query(string name, Type modelType, ICondition criteria, ISchema schema = null, Paging paging = null, params Sorting[] sortings)
		{
			if(modelType == null)
				throw new ArgumentNullException(nameof(modelType));

			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.ModelType = modelType ?? throw new ArgumentNullException(nameof(modelType));
			this.Criteria = criteria;
			this.Schema = schema;
			this.Paging = paging;
			this.Sortings = sortings;
		}

		public Query(string name, Type modelType, Grouping grouping, ICondition criteria, ISchema schema = null, Paging paging = null, params Sorting[] sortings)
		{
			if(modelType == null)
				throw new ArgumentNullException(nameof(modelType));

			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.ModelType = modelType ?? throw new ArgumentNullException(nameof(modelType));
			this.Grouping = grouping;
			this.Criteria = criteria;
			this.Schema = schema;
			this.Paging = paging;
			this.Sortings = sortings;
		}
		#endregion

		#region 公共属性
		/// <summary>获取数据查询的名称。</summary>
		public string Name { get; }

		/// <summary>获取查询要返回的结果集元素类型。</summary>
		public Type ModelType { get; }

		/// <summary>获取或设置查询操作的条件。</summary>
		public ICondition Criteria { get; set; }

		/// <summary>获取或设置查询操作的结果数据模式（即查询结果的形状结构）。</summary>
		public ISchema Schema { get; set; }

		/// <summary>获取或设置查询操作的分组。</summary>
		public Grouping Grouping { get; set; }

		/// <summary>获取或设置查询操作的分页设置。</summary>
		public Paging Paging { get; set; }

		/// <summary>获取或设置查询操作的排序设置。</summary>
		public Sorting[] Sortings { get; set; }
		#endregion
	}
}
