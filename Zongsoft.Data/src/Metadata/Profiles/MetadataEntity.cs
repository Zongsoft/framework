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

namespace Zongsoft.Data.Metadata.Profiles
{
	/// <summary>
	/// 表示数据实体的元数据类。
	/// </summary>
	public class MetadataEntity : DataEntityBase
	{
		#region 构造函数
		public MetadataEntity(string @namespace, string name, string baseName, bool immutable = false) : base(@namespace, name, baseName, immutable)
		{
			this.Properties = new MetadataEntityPropertyCollection(this);
		}
		#endregion

		#region 内部方法
		internal void SetKey(ICollection<string> keys)
		{
			if(keys == null || keys.Count == 0)
				return;

			var index = 0;
			var array = new IDataEntitySimplexProperty[keys.Count];

			foreach(var key in keys)
			{
				if(!this.Properties.TryGet(key, out var property))
					throw new MetadataFileException($"The '{key}' primary key in the '{this.Name}' entity is undefined.");
				if(property.IsComplex)
					throw new MetadataFileException($"The '{key}' primary key in the '{this.Name}' entity cannot be a complex(navigation) property.");

				//将主键属性的是否主键开关打开
				((MetadataEntitySimplexProperty)property).SetPrimaryKey();

				array[index++] = (IDataEntitySimplexProperty)property;
			}

			this.Key = array;
		}
		#endregion
	}
}
