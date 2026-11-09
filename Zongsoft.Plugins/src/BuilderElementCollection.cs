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
using System.Collections.Generic;

namespace Zongsoft.Plugins;

public class BuilderElementCollection : FixedElementCollection
{
	#region 构造函数
	internal BuilderElementCollection() { }
	#endregion

	#region 公共属性
	public BuilderElement this[int index] => (BuilderElement)this.Get(index);
	public BuilderElement this[string name] => (BuilderElement)this.Get(name);
	#endregion

	#region 公共方法
	public BuilderElement Add(string typeName, string name, Plugin plugin)
	{
		var item = new BuilderElement(typeName, name, plugin);
		base.Insert(item, -1);
		return item;
	}
	#endregion
}
