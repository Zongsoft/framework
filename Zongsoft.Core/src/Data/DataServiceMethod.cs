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
	public struct DataServiceMethod : IEquatable<DataServiceMethod>
	{
		#region 公共字段
		/// <summary>方法的名称。</summary>
		public readonly string Name;

		/// <summary>对应的数据访问方法种类。</summary>
		public readonly DataAccessMethod Kind;

		/// <summary>获取一个值，指示该方法是否为批量写入操作。</summary>
		public readonly bool IsMultiple;
		#endregion

		#region 构造函数
		private DataServiceMethod(DataAccessMethod kind)
		{
			this.Kind = kind;
			this.Name = kind.ToString();
			this.IsMultiple = false;
		}

		private DataServiceMethod(string name, DataAccessMethod kind, bool isMultiple)
		{
			this.Name = name ?? kind.ToString();
			this.Kind = kind;
			this.IsMultiple = isMultiple;
		}
		#endregion

		#region 静态方法
		public static DataServiceMethod Get() => new DataServiceMethod(nameof(Get), DataAccessMethod.Select, false);
		public static DataServiceMethod Count() => new DataServiceMethod(nameof(Count), DataAccessMethod.Aggregate, false);
		public static DataServiceMethod Aggregate(DataAggregateFunction aggregate) => new DataServiceMethod(aggregate.ToString(), DataAccessMethod.Aggregate, false);
		public static DataServiceMethod Exists() => new DataServiceMethod(DataAccessMethod.Exists);
		public static DataServiceMethod Execute() => new DataServiceMethod(DataAccessMethod.Execute);
		public static DataServiceMethod Increment() => new DataServiceMethod(nameof(Increment), DataAccessMethod.Increment, false);
		public static DataServiceMethod Decrement() => new DataServiceMethod(nameof(Decrement), DataAccessMethod.Increment, false);
		public static DataServiceMethod Delete() => new DataServiceMethod(DataAccessMethod.Delete);
		public static DataServiceMethod Insert() => new DataServiceMethod(DataAccessMethod.Insert);
		public static DataServiceMethod InsertMany() => new DataServiceMethod(nameof(InsertMany), DataAccessMethod.Insert, true);
		public static DataServiceMethod Update() => new DataServiceMethod(DataAccessMethod.Update);
		public static DataServiceMethod UpdateMany() => new DataServiceMethod(nameof(UpdateMany), DataAccessMethod.Update, true);
		public static DataServiceMethod Upsert() => new DataServiceMethod(DataAccessMethod.Upsert);
		public static DataServiceMethod UpsertMany() => new DataServiceMethod(nameof(UpsertMany), DataAccessMethod.Upsert, true);

		public static DataServiceMethod Select(string name = null)
		{
			if(string.IsNullOrEmpty(name))
				return new DataServiceMethod(DataAccessMethod.Select);
			else
				return new DataServiceMethod(name, DataAccessMethod.Select, false);
		}
		#endregion

		#region 公共方法
		/// <summary>获取一个值，指示当前是否为删除方法，即 <see cref="Kind"/> 属性值是否等于 <see cref="DataAccessMethod.Delete"/>。</summary>
		public bool IsDelete { get => this.Kind == DataAccessMethod.Delete; }
		/// <summary>获取一个值，指示当前是否为新增方法，即 <see cref="Kind"/> 属性值是否等于 <see cref="DataAccessMethod.Insert"/>。</summary>
		public bool IsInsert { get => this.Kind == DataAccessMethod.Insert; }
		/// <summary>获取一个值，指示当前是否为更新方法，即 <see cref="Kind"/> 属性值是否等于 <see cref="DataAccessMethod.Update"/>。</summary>
		public bool IsUpdate { get => this.Kind == DataAccessMethod.Update; }
		/// <summary>获取一个值，指示当前是否为增改方法，即 <see cref="Kind"/> 属性值是否等于 <see cref="DataAccessMethod.Upsert"/>。</summary>
		public bool IsUpsert { get => this.Kind == DataAccessMethod.Upsert; }
		/// <summary>获取一个值，指示当前是否为查询方法，即 <see cref="Kind"/> 属性值是否等于 <see cref="DataAccessMethod.Select"/>。</summary>
		public bool IsSelect { get => this.Kind == DataAccessMethod.Select; }
		/// <summary>获取一个值，指示当前是否为获取方法，即 <see cref="Kind"/> 属性值是否等于 <see cref="DataAccessMethod.Select"/> 并且 <see cref="Name"/> 等于“Get”。</summary>
		public bool IsGet { get => this.Kind == DataAccessMethod.Select && this.Name == nameof(Get); }

		/// <summary>
		/// 获取一个值，指示当前方法是否为读取方法(Get/Select/Exists/Aggregate)。
		/// </summary>
		public bool IsReading
		{
			get => this.Kind == DataAccessMethod.Select ||
				   this.Kind == DataAccessMethod.Exists ||
				   this.Kind == DataAccessMethod.Aggregate;
		}

		/// <summary>
		/// 获取一个值，指示当前方法是否为修改方法(Delete/Insert/Update/Upsert)。
		/// </summary>
		public bool IsWriting
		{
			get => this.Kind == DataAccessMethod.Delete ||
				   this.Kind == DataAccessMethod.Insert ||
				   this.Kind == DataAccessMethod.Update ||
				   this.Kind == DataAccessMethod.Upsert ||
				   this.Kind == DataAccessMethod.Increment;
		}
		#endregion

		#region 重写方法
		public bool Equals(DataServiceMethod method) => this.Kind == method.Kind && string.Equals(this.Name, method.Name);
		public override bool Equals(object obj) => obj is DataServiceMethod method && this.Equals(method);
		public override int GetHashCode() => HashCode.Combine(Name);
		public override string ToString() => this.Name;
		#endregion
	}
}
