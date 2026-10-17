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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Plugins;

public class BuiltinBehavior
{
	#region 构造函数
	public BuiltinBehavior(Builtin builtin, string name, string text = null)
	{
		if(builtin == null)
			throw new ArgumentNullException(nameof(builtin));

		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		this.Builtin = builtin;
		this.Name = name.Trim();
		this.Text = text;
		this.Properties = new PluginElementPropertyCollection(builtin);
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Text { get; set; }
	public Builtin Builtin { get; internal set; }
	public PluginElementPropertyCollection Properties { get; }
	#endregion

	#region 公共方法
	public T GetPropertyValue<T>(string propertyName, T defaultValue = default(T))
	{
		if(string.IsNullOrWhiteSpace(propertyName))
			throw new ArgumentNullException(nameof(propertyName));

		PluginElementProperty property;

		if(this.Properties.TryGetValue(propertyName, out property))
			return (T)property.GetValue(typeof(T), defaultValue);

		return defaultValue;
	}

	public object GetPropertyValue(string propertyName, Type valueType, object defaultValue = null)
	{
		if(string.IsNullOrWhiteSpace(propertyName))
			throw new ArgumentNullException(nameof(propertyName));

		PluginElementProperty property;

		if(this.Properties.TryGetValue(propertyName, out property))
			return property.GetValue(valueType, defaultValue);

		return defaultValue;
	}
	#endregion
}
