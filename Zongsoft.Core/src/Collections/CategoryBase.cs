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

namespace Zongsoft.Collections;

public abstract class CategoryBase<TSelf> : HierarchicalNode<TSelf> where TSelf : CategoryBase<TSelf>
{
	#region 成员字段
	private bool _visible;
	private TSelf _parent;
	private string _title;
	private string _description;
	private readonly Resources.IResource _resource;
	#endregion

	#region 构造函数
	protected CategoryBase() { }
	protected CategoryBase(string name, string title = null, string description = null) : this(null, name, true, title, description) { }
	protected CategoryBase(string name, bool visible, string title = null, string description = null) : this(null, name, visible, title, description) { }

	protected CategoryBase(Resources.IResource resource) => _resource = resource;
	protected CategoryBase(Resources.IResource resource, string name, string title = null, string description = null) : this(resource, name, true, title, description) { }
	protected CategoryBase(Resources.IResource resource, string name, bool visible, string title = null, string description = null) : base(name)
	{
		_resource = resource;
		_visible = visible;
		_title = title;
		_description = description;
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

	public string Title
	{
		get => _title ?? this.GetTitle();
		set => _title = value;
	}

	public string Description
	{
		get => _description ?? this.GetDescription();
		set => _description = value;
	}
	#endregion

	#region 虚拟方法
	protected virtual string GetTitle() => Resources.ResourceUtility.GetString(_resource,
	[
		$"{this.FullPath}.{nameof(this.Title)}",
		this.FullPath,
		$"{this.Name}.{nameof(this.Title)}",
		this.Name,
	]);

	protected virtual string GetDescription() => Resources.ResourceUtility.GetString(_resource,
	[
		$"{this.FullPath}.{nameof(this.Description)}",
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
