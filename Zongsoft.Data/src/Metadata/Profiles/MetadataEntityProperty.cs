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

namespace Zongsoft.Data.Metadata.Profiles
{
	/// <summary>
	/// 表示数据实体属性的元数据抽象基类。
	/// </summary>
	public abstract class MetadataEntityProperty : IDataEntityProperty, IEquatable<IDataEntityProperty>
	{
		#region 构造函数
		protected MetadataEntityProperty(IDataEntity entity, string name, bool immutable)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Entity = entity ?? throw new ArgumentNullException(nameof(entity));
			this.Name = name.Trim();
			this.Immutable = immutable;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取所属的数据实体。
		/// </summary>
		public IDataEntity Entity
		{
			get; internal set;
		}

		/// <summary>
		/// 获取数据实体属性的名称。
		/// </summary>
		public string Name
		{
			get;
		}

		/// <summary>
		/// 获取数据实体属性的别名。
		/// </summary>
		public string Alias
		{
			get; set;
		}

		/// <summary>
		/// 获取或设置数据实体属性是否为不可变属性。
		/// 注：简单属性默认为假(False)，复合属性默认为真(True)。
		/// </summary>
		public bool Immutable
		{
			get; set;
		}

		/// <summary>
		/// 获取一个值，指示数据实体属性是否为主键。
		/// </summary>
		public abstract bool IsPrimaryKey
		{
			get;
		}

		/// <summary>
		/// 获取一个值，指示数据实体属性是否为单值类型。
		/// </summary>
		public abstract bool IsSimplex
		{
			get;
		}

		/// <summary>
		/// 获取一个值，指示数据实体属性是否为复合类型。
		/// </summary>
		public abstract bool IsComplex
		{
			get;
		}
		#endregion

		#region 重写方法
		public virtual bool Equals(IDataEntityProperty other)
		{
			return object.Equals(this.Entity, other.Entity) &&
			       string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj)
		{
			if(obj == null || obj.GetType() != this.GetType())
				return false;

			return this.Equals((IDataEntityProperty)obj);
		}

		public override int GetHashCode()
		{
			return this.Entity.GetHashCode() ^ this.Name.GetHashCode();
		}

		public override string ToString()
		{
			if(this.Entity == null)
				return $"{this.Name}";
			else
				return $"{this.Name}@{this.Entity.Name}";
		}
		#endregion
	}
}
