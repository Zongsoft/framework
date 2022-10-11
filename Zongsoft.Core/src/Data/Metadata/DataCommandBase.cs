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
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata
{
	/// <summary>
	/// 表示数据命令的元数据类。
	/// </summary>
	public class DataCommandBase : IDataCommand, IEquatable<IDataCommand>, IEquatable<DataCommandBase>
	{
		#region 成员字段
		private IDataMetadataContainer _container;
		#endregion

		#region 构造函数
		protected DataCommandBase(string @namespace, string name, string alias = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Namespace = @namespace;
			this.Name = name.Trim();
			this.Alias = alias;
			this.QualifiedName = string.IsNullOrEmpty(@namespace) ? name.Trim().ToLowerInvariant() : $"{@namespace.ToLowerInvariant()}.{name.Trim().ToLowerInvariant()}";
			this.Parameters = new Collections.NamedCollection<IDataCommandParameter>(p => p.Name);
		}
		#endregion

		#region 公共属性
		/// <summary>获取数据命令所属的元数据容器。</summary>
		public virtual IDataMetadataContainer Container
		{
			get => _container;
			set
			{
				if(_container is not null)
					throw new InvalidOperationException();

				_container = value;
			}
		}

		/// <summary>获取所属命名空间。</summary>
		public string Namespace { get; }

		/// <summary>获取数据命令的名称。</summary>
		public string Name { get; }

		/// <summary>获取数据命令的限定名称。</summary>
		public string QualifiedName { get; }

		/// <summary>获取或设置数据命令的类型。</summary>
		public DataCommandType Type { get; set; }

		/// <summary>获取或设置数据命令的别名（表名、存储过程名）。</summary>
		public string Alias { get; set; }

		/// <summary>获取或设置数据命令支持的驱动。</summary>
		public string Driver { get; set; }

		/// <summary>获取或设置数据命令的变化性。</summary>
		public CommandMutability Mutability { get; set; }

		/// <summary>获取数据命令的参数集合。</summary>
		public Collections.INamedCollection<IDataCommandParameter> Parameters { get; }

		/// <summary>获取数据命令的脚本对象。</summary>
		public IDataCommandScriptor Scriptor { get; protected set; }
		#endregion

		#region 重写方法
		public bool Equals(IDataCommand other) => other is not null && string.Equals(this.QualifiedName, other.QualifiedName);
		public bool Equals(DataCommandBase other) => other is not null && string.Equals(this.QualifiedName, other.QualifiedName);
		public override bool Equals(object obj) => obj is DataCommandBase other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.QualifiedName);
		public override string ToString()
		{
			var qualifiedName = string.IsNullOrEmpty(this.Namespace) ?
				$"{this.Name}({(this.Parameters.Count > 0 ? "..." : null)})" :
				$"{this.Namespace}.{this.Name}({(this.Parameters.Count > 0 ? "..." : null)})";

			if(this.Mutability != CommandMutability.None)
				qualifiedName += $"!{this.Mutability}";

			if(this.Container == null || string.IsNullOrEmpty(this.Container.Name))
				return qualifiedName;
			else
				return $"{Container.Name}:{qualifiedName}";
		}
		#endregion
	}
}
