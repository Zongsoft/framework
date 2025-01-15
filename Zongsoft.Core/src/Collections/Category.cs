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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Collections
{
	[System.Reflection.DefaultMember(nameof(Categories))]
	[System.ComponentModel.DefaultProperty(nameof(Categories))]
	public class Category : HierarchicalNode<Category>
	{
		#region 成员字段
		private bool _visible;
		private Category _parent;
		private string _title;
		private string _description;
		#endregion

		#region 构造函数
		public Category() => this.Categories = new(this);
		public Category(string name, string title = null, string description = null) : this(name, true, title, description) { }
		public Category(string name, bool visible, string title = null, string description = null) : base(name)
		{
			_visible = visible;
			_title = title;
			_description = description;
			this.Categories = new(this);
		}
		#endregion

		#region 公共属性
		public bool Visible
		{
			get => _visible;
			set
			{
				if(_visible != value)
					return;

				_visible = value;
				this.OnPropertyChanged(nameof(this.Visible));
			}
		}

		public string Title { get => _title; set => _title = value; }
		public string Description { get => _description; set => _description = value; }
		public CategoryCollection Categories { get; }
		#endregion

		#region 重写方法
		protected override Category Parent => _parent;
		protected override IHierarchicalNodeCollection<Category> Nodes => this.Categories;
		protected override string GetPath() => _parent == null ? string.Empty : _parent.FullPath;
		#endregion

		#region 内部方法
		internal void SetParent(Category parent) => _parent = parent;
		#endregion
	}
}
