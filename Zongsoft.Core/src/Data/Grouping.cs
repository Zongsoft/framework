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
using System.Collections.Generic;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据分组的设置项。
	/// </summary>
	public class Grouping
	{
		#region 成员字段
		private readonly DataAggregateCollection _aggregates;
		#endregion

		#region 构造函数
		private Grouping(ICondition filter, params GroupKey[] keys)
		{
			this.Filter = filter;
			this.Keys = keys ?? Array.Empty<GroupKey>();
			_aggregates = new DataAggregateCollection();
		}
		#endregion

		#region 公共属性
		/// <summary>获取分组键的成员数组。</summary>
		public GroupKey[] Keys { get; }

		/// <summary>获取分组的聚合成员集合。</summary>
		public DataAggregateCollection Aggregates { get => _aggregates; }

		/// <summary>获取或设置分组的过滤条件，默认为空。</summary>
		public ICondition Filter { get; set; }
		#endregion

		#region 公共方法
		public Grouping Count(bool distinct = false)
		{
			_aggregates.Count(null, distinct);
			return this;
		}

		public Grouping Count(string member, bool distinct = false)
		{
			_aggregates.Count(member, distinct);
			return this;
		}

		public Grouping Count(string member, string alias, bool distinct = false)
		{
			_aggregates.Count(member, alias, distinct);
			return this;
		}

		public Grouping Sum(string member, bool distinct = false)
		{
			_aggregates.Sum(member, distinct);
			return this;
		}

		public Grouping Sum(string member, string alias, bool distinct = false)
		{
			_aggregates.Sum(member, alias, distinct);
			return this;
		}

		public Grouping Average(string member, bool distinct = false)
		{
			_aggregates.Average(member, distinct);
			return this;
		}

		public Grouping Average(string member, string alias, bool distinct = false)
		{
			_aggregates.Average(member, alias, distinct);
			return this;
		}

		public Grouping Median(string member, bool distinct = false)
		{
			_aggregates.Median(member, distinct);
			return this;
		}

		public Grouping Median(string member, string alias, bool distinct = false)
		{
			_aggregates.Median(member, alias, distinct);
			return this;
		}

		public Grouping Maximum(string member, bool distinct = false)
		{
			_aggregates.Maximum(member, distinct);
			return this;
		}

		public Grouping Maximum(string member, string alias, bool distinct = false)
		{
			_aggregates.Maximum(member, alias, distinct);
			return this;
		}

		public Grouping Minimum(string member, bool distinct = false)
		{
			_aggregates.Minimum(member, distinct);
			return this;
		}

		public Grouping Minimum(string member, string alias, bool distinct = false)
		{
			_aggregates.Minimum(member, alias, distinct);
			return this;
		}

		public Grouping Deviation(string member, bool distinct = false)
		{
			_aggregates.Deviation(member, distinct);
			return this;
		}

		public Grouping Deviation(string member, string alias, bool distinct = false)
		{
			_aggregates.Deviation(member, alias, distinct);
			return this;
		}

		public Grouping DeviationPopulation(string member, bool distinct = false)
		{
			_aggregates.DeviationPopulation(member, distinct);
			return this;
		}

		public Grouping DeviationPopulation(string member, string alias, bool distinct = false)
		{
			_aggregates.DeviationPopulation(member, alias, distinct);
			return this;
		}

		public Grouping Variance(string member, bool distinct = false)
		{
			_aggregates.Variance(member, distinct);
			return this;
		}

		public Grouping Variance(string member, string alias, bool distinct = false)
		{
			_aggregates.Variance(member, alias, distinct);
			return this;
		}

		public Grouping VariancePopulation(string member, bool distinct = false)
		{
			_aggregates.VariancePopulation(member, distinct);
			return this;
		}

		public Grouping VariancePopulation(string member, string alias, bool distinct = false)
		{
			_aggregates.VariancePopulation(member, alias, distinct);
			return this;
		}
		#endregion

		#region 静态方法
		/// <summary>创建一个分组设置。</summary>
		/// <param name="keys">分组键成员数组，分组键元素使用冒号分隔成员的名称和别名。</param>
		/// <returns>返回创建的分组设置。</returns>
		public static Grouping Group(params string[] keys)
		{
			return Group(null, keys);
		}

		/// <summary>创建一个分组设置。</summary>
		/// <param name="filter">分组的过滤条件。</param>
		/// <param name="keys">分组键成员数组，分组键元素使用冒号分隔成员的名称和别名。</param>
		/// <returns>返回创建的分组设置。</returns>
		public static Grouping Group(ICondition filter, params string[] keys)
		{
			if(keys == null || keys.Length < 1)
				throw new ArgumentNullException(nameof(keys));

			var list = new List<GroupKey>(keys.Length);

			foreach(var key in keys)
			{
				if(string.IsNullOrEmpty(key))
					continue;

				var index = key.IndexOf(':');

				if(index > 0)
					list.Add(new GroupKey(key.Substring(0, index), key.Substring(index + 1)));
				else
					list.Add(new GroupKey(key, null));
			}

			return new Grouping(filter, list.ToArray());
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			var text = new System.Text.StringBuilder();

			if(Keys != null && Keys.Length > 0)
			{
				text.Append("Keys: ");

				foreach(var key in Keys)
				{
					text.Append(key.Name);

					if(key.Alias != null && key.Alias.Length > 0)
						text.Append(" '" + key.Alias + "'");
				}

				text.AppendLine();
			}

			if(_aggregates != null)
			{
				foreach(var aggregate in _aggregates)
				{
					text.Append(aggregate.Function.ToString() + ": " + aggregate.Name);

					if(aggregate.Alias != null && aggregate.Alias.Length > 0)
						text.Append(" '" + aggregate.Alias + "'");

					text.AppendLine();
				}
			}

			if(Filter != null)
			{
				text.AppendLine("Filter: " + Filter.ToString());
			}

			if(text == null)
				return string.Empty;
			else
				return text.ToString();
		}
		#endregion

		#region 嵌套结构
		/// <summary>
		/// 表示数据分组键的结构。
		/// </summary>
		public readonly struct GroupKey : IEquatable<GroupKey>
		{
			#region 公共字段
			/// <summary>获取分组键名。</summary>
			public readonly string Name;
			/// <summary>获取分组键的别名。</summary>
			public readonly string Alias;
			#endregion

			#region 构造函数
			public GroupKey(string name, string alias = null)
			{
				this.Name = name ?? throw new ArgumentNullException(nameof(name));
				this.Alias = string.IsNullOrWhiteSpace(alias) ? null : alias;
			}
			#endregion

			#region 重写方法
			public bool Equals(GroupKey other) => string.Equals(this.Name, other.Name) && string.Equals(this.Alias, other.Alias);
			public override bool Equals(object obj) => obj is GroupKey other && this.Equals(other);
			public override int GetHashCode() => HashCode.Combine(this.Name, this.Alias);
			public override string ToString() => string.IsNullOrEmpty(this.Alias) ? this.Name : $"{this.Name}:{this.Alias}";
			#endregion

			#region 重写符号
			public static bool operator ==(GroupKey left, GroupKey right) => left.Equals(right);
			public static bool operator !=(GroupKey left, GroupKey right) => !(left == right);
			#endregion
		}
		#endregion
	}
}
