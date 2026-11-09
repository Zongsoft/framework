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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

namespace Zongsoft.Collections;

[System.Reflection.DefaultMember(nameof(Categories))]
[System.ComponentModel.DefaultProperty(nameof(Categories))]
public class Category : CategoryBase<Category>
{
	#region 构造函数
	public Category() => this.Categories = new(this);
	public Category(string name, string title = null, string description = null) : this(null, name, title, description) { }

	public Category(Resources.IResource resource) : base(resource) => this.Categories = new(this);
	public Category(Resources.IResource resource, string name, string title = null, string description = null) : base(resource, name, title, description)
	{
		this.Categories = new(this);
	}
	#endregion

	#region 公共属性
	public CategoryCollection Categories { get; }
	#endregion

	#region 重写方法
	protected override IHierarchicalNodeCollection<Category> Nodes => this.Categories;
	#endregion
}
