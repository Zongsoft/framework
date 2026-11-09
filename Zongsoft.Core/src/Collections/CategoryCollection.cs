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

public class CategoryCollection : CategoryCollectionBase<Category>
{
	#region 构造函数
	public CategoryCollection() : base(null) { }
	internal CategoryCollection(Category owner) : base(owner) { }
	#endregion

	#region 公共方法
	public Category Add(string name, string title = null, string description = null)
	{
		var category = new Category(name, title, description);
		this.Add(category);
		return category;
	}
	#endregion
}
