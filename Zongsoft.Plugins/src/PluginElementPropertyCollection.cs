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
using System.Collections.ObjectModel;

namespace Zongsoft.Plugins;

public class PluginElementPropertyCollection : KeyedCollection<string, PluginElementProperty>
{
	#region 成员字段
	private readonly PluginElement _owner;
	#endregion

	#region 构造函数
	public PluginElementPropertyCollection(PluginElement owner) : base(StringComparer.OrdinalIgnoreCase)
	{
		_owner = owner ?? throw new ArgumentNullException(nameof(owner));
	}
	#endregion

	#region 公共方法
	public void Set(string name, string rawValue)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		if(this.TryGetValue(name, out var property))
			property.RawValue = rawValue;
		else
			this.Add(new PluginElementProperty(_owner, name, rawValue));
	}
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(PluginElementProperty item) => item.Name;
	protected override void InsertItem(int index, PluginElementProperty item)
	{
		if(item == null)
			throw new ArgumentNullException(nameof(item));

		item.Owner = _owner;
		base.InsertItem(index, item);
	}
	#endregion
}
