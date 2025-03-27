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
	public PrivilegeCategory(string name, string title = null, string description = null) : this(null, name, title, description) { }
	public PrivilegeCategory(IResource resource, string name, string title = null, string description = null) : base(resource, name, title, description)
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

	/// <summary>获取权限类别的本地化标题。</summary>
	/// <returns>返回本地化标题文本，如果失败则返回空(<c>null</c>)。</returns>
	/// <remarks>
	///		<para>标题对应的资源键按优先顺序，依次如下：</para>
	///		<para>提示：其 <c>{name}</c> 表示 <see cref="HierarchicalNode.Name"/> 属性的值；<c>{path}</c> 表示 <see cref="HierarchicalNode.FullPath"/> 属性的值。</para>
	///		<list type="number">
	///			<item>Privilege.{path}.Category.Title</item>
	///			<item>Privilege.{path}.Category</item>
	///			<item>Privilege.{path}.Title</item>
	///			<item>Privilege.{path}</item>
	///			<item>Privilege.{name}.Category.Title</item>
	///			<item>Privilege.{name}.Category</item>
	///			<item>Privilege.{name}.Title</item>
	///			<item>Privilege.{name}</item>
	///			<item>{path}.Category.Title</item>
	///			<item>{path}.Category</item>
	///			<item>{path}.Title</item>
	///			<item>{path}</item>
	///			<item>{name}.Category.Title</item>
	///			<item>{name}.Category</item>
	///			<item>{name}.Title</item>
	///			<item>{name}</item>
	///		</list>
	/// </remarks>
	protected override string GetTitle() => this.Resource.GetString(
	[
		$"{nameof(Privilege)}.{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}.{nameof(Category)}.{nameof(this.Title)}",
		$"{nameof(Privilege)}.{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}.{nameof(Category)}",
		$"{nameof(Privilege)}.{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}.{nameof(this.Title)}",
		$"{nameof(Privilege)}.{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}",
		$"{nameof(Privilege)}.{this.Name}.{nameof(Category)}.{nameof(this.Title)}",
		$"{nameof(Privilege)}.{this.Name}.{nameof(Category)}",
		$"{nameof(Privilege)}.{this.Name}.{nameof(this.Title)}",
		$"{nameof(Privilege)}.{this.Name}",
	]) ?? base.GetTitle();

	/// <summary>获取权限类别的本地化描述。</summary>
	/// <returns>返回本地化描述文本，如果失败则返回空(<c>null</c>)。</returns>
	/// <remarks>
	///		<para>对应的资源键按优先顺序，依次如下：</para>
	///		<para>提示：其 <c>{name}</c> 表示 <see cref="HierarchicalNode.Name"/> 属性的值；<c>{path}</c> 表示 <see cref="HierarchicalNode.FullPath"/> 属性的值。</para>
	///		<list type="number">
	///			<item>Privilege.{path}.Category.Description</item>
	///			<item>Privilege.{path}.Description</item>
	///			<item>Privilege.{name}.Category.Description</item>
	///			<item>Privilege.{name}.Description</item>
	///			<item>{path}.Category.Description</item>
	///			<item>{path}.Description</item>
	///			<item>{name}.Category.Description</item>
	///			<item>{name}.Description</item>
	///		</list>
	/// </remarks>
	protected override string GetDescription() => this.Resource.GetString(
	[
		$"{nameof(Privilege)}.{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}.{nameof(Category)}.{nameof(this.Description)}",
		$"{nameof(Privilege)}.{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}.{nameof(this.Description)}",
		$"{nameof(Privilege)}.{this.Name}.{nameof(Category)}.{nameof(this.Description)}",
		$"{nameof(Privilege)}.{this.Name}.{nameof(this.Description)}",
	]) ?? base.GetDescription();
	#endregion

	#region 显式实现
	object IDiscriminator.Discriminate(object argument)
	{
		switch(argument)
		{
			case string type:
				if(string.IsNullOrEmpty(type) || string.Equals(type, nameof(Category), StringComparison.OrdinalIgnoreCase))
					return this.Categories;

				if(string.Equals(type, nameof(Privilege), StringComparison.OrdinalIgnoreCase))
					return this.Privileges;

				break;
			case Privilege:
				return this.Privileges;
			case PrivilegeCategory:
				return this.Categories;
		}

		return null;
	}
	#endregion
}
