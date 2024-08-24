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

using Zongsoft.Collections;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data
{
	public class SchemaMember : SchemaMemberBase
	{
		#region 成员字段
		private SchemaMember _parent;
		private INamedCollection<SchemaMember> _children;
		#endregion

		#region 构造函数
		internal SchemaMember(IDataEntityProperty property, IEnumerable<IDataEntity> ancestors = null)
		{
			this.Token = new DataEntityPropertyToken(property);
			this.Ancestors = ancestors;
		}

		internal SchemaMember(DataEntityPropertyToken token, IEnumerable<IDataEntity> ancestors = null)
		{
			this.Token = token;
			this.Ancestors = ancestors;
		}
		#endregion

		#region 公共属性
		public override string Name
		{
			get
			{
				return this.Token.Property.Name;
			}
		}

		public DataEntityPropertyToken Token
		{
			get;
		}

		public SchemaMember Parent
		{
			get
			{
				return _parent;
			}
		}

		public IEnumerable<IDataEntity> Ancestors
		{
			get;
		}

		public override bool HasChildren
		{
			get
			{
				return _children != null && _children.Count > 0;
			}
		}

		public INamedCollection<SchemaMember> Children
		{
			get
			{
				return _children;
			}
		}
		#endregion

		#region 重写方法
		protected override SchemaMemberBase GetParent()
		{
			return _parent;
		}

		protected override void SetParent(SchemaMemberBase parent)
		{
			_parent = (parent as SchemaMember) ?? throw new ArgumentException();
		}

		protected override bool TryGetChild(string name, out SchemaMemberBase child)
		{
			child = null;

			if(_children != null && _children.TryGet(name, out var schema))
			{
				child = schema;
				return true;
			}

			return false;
		}

		protected override void AddChild(SchemaMemberBase child)
		{
			if(!(child is SchemaMember schema))
				throw new ArgumentNullException();

			if(_children == null)
				System.Threading.Interlocked.CompareExchange(ref _children, new NamedCollection<SchemaMember>(item => item.Name), null);

			_children.Add(schema);
			schema._parent = this;
		}

		protected override void RemoveChild(string name)
		{
			_children?.Remove(name);
		}

		protected override void ClearChildren()
		{
			_children?.Clear();
		}

		public override string ToString()
		{
			var index = 0;
			var text = this.Name;

			if(this.Paging != null)
			{
				if(Paging.IsDisabled(this.Paging))
					text += ":*";
				else
					text += ":" + (this.Paging.PageIndex == 1 ?
								   this.Paging.PageSize.ToString() :
								   this.Paging.PageIndex.ToString() + "/" + this.Paging.PageSize.ToString());
			}

			if(this.Sortings != null && this.Sortings.Length > 0)
			{
				index = 0;
				text += "(";

				foreach(var sorting in this.Sortings)
				{
					if(index++ > 0)
						text += ", ";

					if(sorting.Mode == SortingMode.Ascending)
						text += sorting.Name;
					else
						text += "~" + sorting.Name;
				}

				text += ")";
			}

			if(_children != null && _children.Count > 0)
			{
				index = 0;
				text += "{";

				foreach(var child in _children)
				{
					if(index++ > 0)
						text += ", ";

					text += child.ToString();
				}

				text += "}";
			}

			return text;
		}
		#endregion

		internal void Append(SchemaMember child)
		{
			this.AddChild(child);
		}
	}
}
