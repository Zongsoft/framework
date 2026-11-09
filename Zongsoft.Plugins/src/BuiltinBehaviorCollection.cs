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

public class BuiltinBehaviorCollection(Builtin builtin) : KeyedCollection<string, BuiltinBehavior>(StringComparer.OrdinalIgnoreCase)
{
	#region 成员字段
	private readonly Builtin _builtin = builtin ?? throw new ArgumentNullException(nameof(builtin));
	#endregion

	#region 公共方法
	public BuiltinBehavior Add(string name, string text = null)
	{
		var result = new BuiltinBehavior(_builtin, name, text);
		this.Add(result);
		return result;
	}

	public bool TryGet(string name, out BuiltinBehavior value)
	{
		value = null;

		var dictionary = this.Dictionary;

		if(dictionary != null)
			return dictionary.TryGetValue(name, out value);

		foreach(var item in this.Items)
		{
			if(this.Comparer.Equals(this.GetKeyForItem(item), name))
			{
				value = item;
				return true;
			}
		}

		return false;
	}

	public T GetBehaviorValue<T>(string name, T defaultValue = default)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		var index = name.IndexOf('.');

		if(index < 0 || index >= name.Length - 1)
			throw new ArgumentException();

		if(this.TryGet(name[..index], out var behavior))
			return behavior.GetPropertyValue<T>(name[(index + 1)..], defaultValue);

		return defaultValue;
	}
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(BuiltinBehavior item) => item.Name;
	protected override void InsertItem(int index, BuiltinBehavior item)
	{
		if(item == null)
			throw new ArgumentNullException(nameof(item));

		//设置属性的所有者
		item.Builtin = _builtin;

		//调用基类同名方法
		base.InsertItem(index, item);
	}
	#endregion
}
