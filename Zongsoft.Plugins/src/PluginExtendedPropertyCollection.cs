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

public class PluginExtendedPropertyCollection : KeyedCollection<string, PluginExtendedProperty>
{
	#region 成员变量
	private readonly PluginElement _owner;
	#endregion

	#region 构造函数
	public PluginExtendedPropertyCollection(PluginElement owner) : base(StringComparer.OrdinalIgnoreCase)
	{
		_owner = owner ?? throw new ArgumentNullException(nameof(owner));
	}
	#endregion

	#region 公共属性
	public PluginElement Owner => _owner;
	#endregion

	#region 公共方法
	public object GetValue(string name, Type type, object defaultValue)
	{
		if(this.TryGetProperty(name, out var property))
			return property.GetValue(type, defaultValue);

		return defaultValue;
	}

	public T GetValue<T>(string name)
	{
		return this.GetValue<T>(name, default(T));
	}

	public T GetValue<T>(string name, T defaultValue)
	{
		if(this.TryGetProperty(name, out var property))
			return (T)property.GetValue(typeof(T), defaultValue);

		return defaultValue;
	}

	public string GetRawValue(string name)
	{
		if(this.TryGetProperty(name, out var property))
			return property.RawValue;

		return null;
	}

	public bool TryGetValue(string name, Type valueType, out object value)
	{
		if(this.TryGetProperty(name, out var property))
		{
			value = property.GetValue(valueType);
			return true;
		}

		value = null;
		return false;
	}
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(PluginExtendedProperty item) => item.Name;
	#endregion

	#region 内部方法
	internal PluginExtendedProperty Set(string name, object value, Plugin plugin = null)
	{
		var property = value switch
		{
			Builtin builtin => new PluginExtendedProperty(_owner, name, builtin.Node, plugin ?? builtin.Plugin),
			PluginTreeNode node => new PluginExtendedProperty(_owner, name, node, plugin ?? node.Plugin),
			string text => new PluginExtendedProperty(_owner, name, text, plugin ?? _owner.Plugin),
			_ => throw new PluginException(Properties.Resources.Plugin_PropertyInvalid_Message),
		};

		if(this.Contains(name))
			this.Remove(name);

		this.Add(property);
		return property;
	}
	#endregion

	#region 私有方法
	private bool TryGetProperty(string name, out PluginExtendedProperty property)
	{
		if(this.TryGetValue(name, out property))
			return true;

		if(_owner is PluginTreeNode node && node.NodeType == PluginTreeNodeType.Builtin)
		{
			if(((Builtin)node.Value).Properties.TryGetValue(name, out property))
				return true;
		}

		property = null;
		return false;
	}
	#endregion
}
