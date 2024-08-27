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

namespace Zongsoft.Data.Metadata
{
	/// <summary>
	/// 表示数据实体关联成员的元数据类。
	/// </summary>
	public class DataAssociationLink
	{
		#region 成员字段
		private IDataEntityComplexProperty _owner;
		private IDataEntitySimplexProperty _foreignKey;
		private readonly string _foreign;
		private readonly string _anchor;
		#endregion

		#region 构造函数
		public DataAssociationLink(IDataEntityComplexProperty owner, string foreign, string anchor = null)
		{
			if(string.IsNullOrEmpty(foreign))
				throw new ArgumentNullException(nameof(foreign));

			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
			_foreign = foreign;
			_anchor = string.IsNullOrEmpty(anchor) ? foreign : anchor;
			_foreignKey = null;
		}
		#endregion

		#region 公共属性
		/// <summary>获取关联元素的外键属性。</summary>
		public IDataEntitySimplexProperty ForeignKey
		{
			get
			{
				if(_foreignKey == null)
					_foreignKey = (IDataEntitySimplexProperty)_owner.Foreign.Properties[_foreign];

				return _foreignKey;
			}
		}

		/// <summary>获取关联元素的外键属性名。</summary>
		public string Foreign { get => _foreign; }

		/// <summary>获取关联元素的锚点。</summary>
		public string Anchor { get => _anchor; }
		#endregion

		#region 公共方法
		public IDataEntityProperty[] GetAnchors()
		{
			var parts = _anchor.Split('.');
			var result = new IDataEntityProperty[parts.Length];
			var entity = _owner.Entity;
			IDataEntityProperty property;

			for(int i = 0; i < parts.Length - 1; i++)
			{
				if(entity.Properties.TryGetValue(parts[i], out property) && property.IsComplex)
				{
					result[i] = property;
					entity = ((IDataEntityComplexProperty)property).Foreign;
				}
				else
					throw new DataException($"The link anchor value '{_anchor}' in the '{_owner}' complex property is invalid.");
			}

			if(entity.Properties.TryGetValue(parts[^1], out property) && property.IsSimplex)
				result[^1] = property;
			else
				throw new DataException($"The link anchor value '{_anchor}' in the '{_owner}' complex property is invalid.");

			return result;
		}
		#endregion

		#region 重写方法
		public override string ToString() => _foreign + "=" + _anchor;
		#endregion
	}
}
