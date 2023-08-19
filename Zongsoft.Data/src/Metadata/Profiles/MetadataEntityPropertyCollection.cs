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
	/// 表示数据实体属性元数据的集合类。
	/// </summary>
	public class MetadataEntityPropertyCollection : Zongsoft.Collections.NamedCollectionBase<IDataEntityProperty>, IDataEntityPropertyCollection
	{
		#region 构造函数
		public MetadataEntityPropertyCollection(IDataEntity entity) : base()
		{
			this.Entity = entity ?? throw new ArgumentNullException(nameof(entity));
		}
		#endregion

		#region 公共属性
		public IDataEntity Entity { get; }
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(IDataEntityProperty property) => property.Name;
		protected override void AddItem(IDataEntityProperty item)
		{
			if(item == null)
				throw new ArgumentNullException(nameof(item));

			if(this.Contains(item.Name))
				throw new DataException($"The specified '{item}' property already exists in the '{this.Entity}' entity.");

			if(item is MetadataEntityProperty property)
				property.Entity = this.Entity;

			//调用基类同名方法
			base.AddItem(item);
		}
		protected override void SetItem(string name, IDataEntityProperty item)
		{
			if(item is MetadataEntityProperty property)
				property.Entity = this.Entity;

			//调用基类同名方法
			base.SetItem(name, item);
		}
		#endregion
	}
}
