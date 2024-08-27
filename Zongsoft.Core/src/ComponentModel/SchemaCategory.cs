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

namespace Zongsoft.ComponentModel
{
	[System.Reflection.DefaultMember(nameof(Schemas))]
	public class SchemaCategory : Zongsoft.Collections.HierarchicalNode<SchemaCategory>
	{
		#region 静态字段
		public static readonly SchemaCategory Default = new SchemaCategory();
		#endregion

		#region 成员字段
		private SchemaCollection _schemas;
		private SchemaCategoryCollection _children;
		#endregion

		#region 构造函数
		public SchemaCategory()
		{
			_schemas = new SchemaCollection();
		}

		public SchemaCategory(string name) : base(name, name, string.Empty)
		{
			_schemas = new SchemaCollection();
		}

		public SchemaCategory(string name, string title, string description) : base(name, title, description)
		{
			_schemas = new SchemaCollection();
		}
		#endregion

		#region 公共属性
		public SchemaCategoryCollection Children
		{
			get
			{
				if(_children == null)
					System.Threading.Interlocked.CompareExchange(ref _children, new SchemaCategoryCollection(this), null);

				return _children;
			}
		}

		public SchemaCollection Schemas
		{
			get
			{
				return _schemas;
			}
		}
		#endregion

		#region 重写方法
		protected override Collections.HierarchicalNode GetChild(string name)
		{
			return _children != null && _children.TryGetValue(name, out var child) ? child : null;
		}
		#endregion
	}
}
