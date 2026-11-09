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

namespace Zongsoft.Plugins;

public class FixedElementCollection<T> : FixedElementCollection
{
	#region 构造函数
	internal protected FixedElementCollection() { }
	#endregion

	#region 公共属性
	public FixedElementType ElementType
	{
		get
		{
			if(typeof(IParser).IsAssignableFrom(typeof(T)))
				return FixedElementType.Parser;
			if(typeof(IBuilder).IsAssignableFrom(typeof(T)))
				return FixedElementType.Builder;

			throw new PluginException();
		}
	}

	public FixedElement<T> this[int index] => (FixedElement<T>)this.Get(index);
	public FixedElement<T> this[string name] => (FixedElement<T>)this.Get(name);
	#endregion

	#region 公共方法
	public FixedElement<T> Add(string typeName, string name, Plugin plugin)
	{
		var item = new FixedElement<T>(typeName, name, plugin, this.ElementType);
		base.Insert(item, -1);
		return item;
	}

	public FixedElement<T> Add(Type type, string name, Plugin plugin)
	{
		var item = new FixedElement<T>(type, name, plugin, this.ElementType);
		base.Insert(item, -1);
		return item;
	}
	#endregion
}
