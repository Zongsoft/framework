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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Resources;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

public class PrivilegeCategory : CategoryBase<PrivilegeCategory>, IDiscriminator
{
	#region 内部构造
	internal PrivilegeCategory()
	{
		this.Privileges = new(this);
		this.Categories = new(this);
	}
	#endregion

	#region 公共构造
	public PrivilegeCategory(string name, string title = null, string description = null) : this(null, name, true, title, description) { }
	public PrivilegeCategory(string name, bool visible, string title = null, string description = null) : this(null, name, visible, title, description) { }
	public PrivilegeCategory(IResource resource, string name, string title = null, string description = null) : this(resource, name, true, title, description) { }
	public PrivilegeCategory(IResource resource, string name, bool visible, string title = null, string description = null) : base(resource, name, visible, title, description)
	{
		this.Privileges = new(this);
		this.Categories = new(this);
	}
	#endregion

	#region 公共属性
	public PrivilegeCollection Privileges { get; }
	public PrivilegeCategoryCollection Categories { get; }
	public Privilege this[string name] => this.Privileges[name];
	#endregion

	#region 重写方法
	protected override IHierarchicalNodeCollection<PrivilegeCategory> Nodes => this.Categories;
	#endregion

	#region 显式实现
	object IDiscriminator.Discriminate(object argument)
	{
		if(argument == null)
			return this.Categories;

		if(argument is string type)
		{
			if(string.IsNullOrEmpty(type) || string.Equals(type, nameof(Category), StringComparison.OrdinalIgnoreCase))
				return this.Categories;

			if(string.Equals(type, nameof(Privilege), StringComparison.OrdinalIgnoreCase))
				return this.Privileges;
		}

		return null;
	}
	#endregion
}
