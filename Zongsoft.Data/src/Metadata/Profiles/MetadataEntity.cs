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
	/// 表示数据实体的元数据类。
	/// </summary>
	public class MetadataEntity : IDataEntity, IEquatable<IDataEntity>
	{
		#region 成员字段
		private bool? _hasSequences;
		private IDataEntityPropertySequence[] _sequences;
		#endregion

		#region 构造函数
		public MetadataEntity(IDataMetadataProvider metadata, string @namespace, string name, string baseName, bool immutable = false)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Namespace = @namespace;
			this.Name = name.Trim();
			this.BaseName = baseName;
			this.Immutable = immutable;
			this.Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			this.Properties = new MetadataEntityPropertyCollection(this);
		}
		#endregion

		#region 公共属性
		/// <summary>获取数据实体所属的提供程序。</summary>
		public IDataMetadataProvider Metadata { get; }

		/// <summary>获取所属命名空间。</summary>
		public string Namespace { get; }

		/// <summary>获取数据实体的名称。</summary>
		public string Name { get; }

		/// <summary>获取或设置数据实体的别名。</summary>
		public string Alias { get; set; }

		/// <summary>获取或设置数据实体继承的父实体名。</summary>
		public string BaseName { get; set; }

		/// <summary>获取或设置数据实体的主键属性数组。</summary>
		public IDataEntitySimplexProperty[] Key { get; set; }

		/// <summary>获取或设置一个值，指示是否为不可变实体。</summary>
		public bool Immutable { get; set; }

		/// <summary>获取一个值，指示该实体定义中是否含有序号属性。</summary>
		public bool HasSequences
		{
			get
			{
				if(_hasSequences == null)
				{
					foreach(var property in this.Properties)
					{
						if(property.IsSimplex && ((IDataEntitySimplexProperty)property).Sequence != null)
							return (_hasSequences = true).Value;
					}

					_hasSequences = false;
				}

				return _hasSequences.Value;
			}
		}

		/// <summary>获取数据实体的属性元数据集合。</summary>
		public IDataEntityPropertyCollection Properties { get; }
		#endregion

		#region 公共方法
		public IDataEntityPropertySequence[] GetSequences()
		{
			if(_sequences == null)
			{
				var sequences = new List<IDataEntityPropertySequence>();

				foreach(var property in this.Properties)
				{
					if(property.IsSimplex && ((IDataEntitySimplexProperty)property).Sequence != null)
						sequences.Add(((IDataEntitySimplexProperty)property).Sequence);
				}

				_sequences = sequences.ToArray();
			}

			return _sequences;
		}
		#endregion

		#region 重写方法
		public bool Equals(IDataEntity other)
		{
			return other != null && string.Equals(other.Name, Name) && string.Equals(other.Alias, Alias);
		}

		public override bool Equals(object obj)
		{
			if(obj == null || obj.GetType() != this.GetType())
				return false;

			return this.Equals((IDataEntity)obj);
		}

		public override int GetHashCode()
		{
			if(string.IsNullOrEmpty(Alias))
				return Name.GetHashCode();
			else
				return Name.GetHashCode() ^ Alias.GetHashCode();
		}

		public override string ToString()
		{
			var name = this.Name;

			if(this.Immutable)
				name += "(Immutable)";

			if(string.IsNullOrEmpty(this.BaseName))
				return $"{name}@{this.Metadata}";
			else
				return $"{name}:{this.BaseName}@{this.Metadata}";
		}
		#endregion
	}
}
