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
using System.ComponentModel;

namespace Zongsoft.Collections;

public abstract class CategoryBase<TSelf> : HierarchicalNode<TSelf> where TSelf : CategoryBase<TSelf>
{
	#region 成员字段
	private TSelf _parent;
	private string _icon;
	private string _title;
	private string _description;
	private string[] _tags;
	private readonly Resources.IResource _resource;
	#endregion

	#region 构造函数
	protected CategoryBase() { }
	protected CategoryBase(string name, string title = null, string description = null) : this(null, name, title, description) { }

	protected CategoryBase(Resources.IResource resource) => _resource = resource;
	protected CategoryBase(Resources.IResource resource, string name, string title = null, string description = null) : base(name)
	{
		_resource = resource;
		_title = title;
		_description = description;
	}
	#endregion

	#region 公共属性
	public string Icon
	{
		get => _icon;
		set
		{
			_icon = value;
			this.OnPropertyChanged(nameof(this.Icon));
		}
	}

	public string Title
	{
		get => _title ?? this.GetTitle();
		set
		{
			_title = value;
			this.OnPropertyChanged(nameof(this.Title));
		}
	}

	public string Description
	{
		get => _description ?? this.GetDescription();
		set
		{
			_description = value;
			this.OnPropertyChanged(nameof(this.Description));
		}
	}

	[TypeConverter(typeof(Components.Converters.CollectionConverter))]
	public string[] Tags
	{
		get => _tags;
		set
		{
			_tags = value;
			this.OnPropertyChanged(nameof(this.Tags));
		}
	}
	#endregion

	#region 保护属性
	protected Resources.IResource Resource => _resource;
	#endregion

	#region 虚拟方法
	/// <summary>获取类别的本地化标题。</summary>
	/// <returns>返回本地化标题文本，如果失败则返回空(<c>null</c>)。</returns>
	/// <remarks>
	///		<para>对应的资源键按优先顺序，依次如下：</para>
	///		<para>提示：其 <c>{name}</c> 表示 <see cref="HierarchicalNode.Name"/> 属性的值；<c>{path}</c> 表示 <see cref="HierarchicalNode.FullPath"/> 属性的值。</para>
	///		<list type="number">
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
	protected virtual string GetTitle() => Resources.ResourceUtility.GetString(_resource,
	[
		$"{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}.{nameof(Category)}.{nameof(this.Title)}",
		$"{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}.{nameof(Category)}",
		$"{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}.{nameof(this.Title)}",
		this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.'),
		$"{this.Name}.{nameof(Category)}.{nameof(this.Title)}",
		$"{this.Name}.{nameof(Category)}",
		$"{this.Name}.{nameof(this.Title)}",
		this.Name,
	]);

	/// <summary>获取类别的本地化描述。</summary>
	/// <returns>返回本地化描述文本，如果失败则返回空(<c>null</c>)。</returns>
	/// <remarks>
	///		<para>对应的资源键按优先顺序，依次如下：</para>
	///		<para>提示：其 <c>{name}</c> 表示 <see cref="HierarchicalNode.Name"/> 属性的值；<c>{path}</c> 表示 <see cref="HierarchicalNode.FullPath"/> 属性的值。</para>
	///		<list type="number">
	///			<item>{path}.Category.Description</item>
	///			<item>{path}.Description</item>
	///			<item>{name}.Category.Description</item>
	///			<item>{name}.Description</item>
	///		</list>
	/// </remarks>
	protected virtual string GetDescription() => Resources.ResourceUtility.GetString(_resource,
	[
		$"{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}.{nameof(Category)}.{nameof(this.Description)}",
		$"{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}.{nameof(this.Description)}",
		$"{this.Name}.{nameof(Category)}.{nameof(this.Description)}",
		$"{this.Name}.{nameof(this.Description)}",
	]);
	#endregion

	#region 重写方法
	protected override TSelf Parent => _parent;
	protected override string GetPath() => _parent == null ? string.Empty : _parent.FullPath;
	#endregion

	#region 内部方法
	internal void SetParent(TSelf parent) => _parent = parent;
	#endregion
}
