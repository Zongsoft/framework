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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Zongsoft.ComponentModel;

namespace Zongsoft.Configuration;

public class ConnectionSettingDescriptorCollection() : KeyedCollection<string, ConnectionSettingDescriptor>(StringComparer.OrdinalIgnoreCase)
{
	#region 重写方法
	protected override string GetKeyForItem(ConnectionSettingDescriptor descriptor) => descriptor.Name;

	protected override void InsertItem(int index, ConnectionSettingDescriptor descriptor)
	{
		if(descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));

		base.InsertItem(index, descriptor);

		if(!string.IsNullOrEmpty(descriptor.Alias))
			this.Dictionary.TryAdd(descriptor.Alias, descriptor);
	}

	protected override void SetItem(int index, ConnectionSettingDescriptor descriptor)
	{
		if(descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));

		var older = this.Items[index];
		if(!string.IsNullOrEmpty(older.Alias))
			this.Dictionary.Remove(older.Alias);

		if(!string.IsNullOrEmpty(descriptor.Alias))
			this.Dictionary.TryAdd(descriptor.Alias, descriptor);

		base.SetItem(index, descriptor);
	}

	protected override void RemoveItem(int index)
	{
		var older = this.Items[index];
		if(!string.IsNullOrEmpty(older.Alias))
			this.Dictionary.Remove(older.Alias);

		base.RemoveItem(index);
	}
	#endregion

	#region 公共方法
	public ConnectionSettingDescriptor Add(string name, string label = null, string description = null) => this.Add(name, false, null, null, label, description);
	public ConnectionSettingDescriptor Add(string name, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null) => this.Add(name, false, null, dependencies, label, description);
	public ConnectionSettingDescriptor Add(string name, object defaultValue, string label = null, string description = null) => this.Add(name, false, defaultValue, null, label, description);
	public ConnectionSettingDescriptor Add(string name, object defaultValue, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null) => this.Add(name, false, defaultValue, dependencies, label, description);
	public ConnectionSettingDescriptor Add(string name, bool required, string label = null, string description = null) => this.Add(name, required, null, null, label, description);
	public ConnectionSettingDescriptor Add(string name, bool required, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null) => this.Add(name, required, null, dependencies, label, description);
	public ConnectionSettingDescriptor Add(string name, bool required, object defaultValue, string label = null, string description = null) => this.Add(name, required, defaultValue, null, label, description);
	public ConnectionSettingDescriptor Add(string name, bool required, object defaultValue, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null)
	{
		var descriptor = new ConnectionSettingDescriptor(name, null, null, required, defaultValue, label, description, dependencies);
		this.Add(descriptor);
		return descriptor;
	}

	public ConnectionSettingDescriptor Add(string name, Type type, string label = null, string description = null) => this.Add(name, type, false, null, null, label, description);
	public ConnectionSettingDescriptor Add(string name, Type type, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null) => this.Add(name, type, false, null, dependencies, label, description);
	public ConnectionSettingDescriptor Add(string name, Type type, object defaultValue, string label = null, string description = null) => this.Add(name, type, false, defaultValue, null, label, description);
	public ConnectionSettingDescriptor Add(string name, Type type, object defaultValue, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null) => this.Add(name, type, false, defaultValue, dependencies, label, description);
	public ConnectionSettingDescriptor Add(string name, Type type, bool required, string label = null, string description = null) => this.Add(name, type, required, null, null, label, description);
	public ConnectionSettingDescriptor Add(string name, Type type, bool required, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null) => this.Add(name, type, required, null, dependencies, label, description);
	public ConnectionSettingDescriptor Add(string name, Type type, bool required, object defaultValue, string label = null, string description = null) => this.Add(name, type, required, defaultValue, null, label, description);
	public ConnectionSettingDescriptor Add(string name, Type type, bool required, object defaultValue, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null)
	{
		var descriptor = new ConnectionSettingDescriptor(name, null, type, required, defaultValue, label, description, dependencies);
		this.Add(descriptor);
		return descriptor;
	}

	public ConnectionSettingDescriptor Add(string name, string alias, string label = null, string description = null) => this.Add(name, alias, false, null, null, label, description);
	public ConnectionSettingDescriptor Add(string name, string alias, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null) => this.Add(name, alias, false, null, dependencies, label, description);
	public ConnectionSettingDescriptor Add(string name, string alias, object defaultValue, string label = null, string description = null) => this.Add(name, alias, false, defaultValue, null, label, description);
	public ConnectionSettingDescriptor Add(string name, string alias, object defaultValue, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null) => this.Add(name, alias, false, defaultValue, dependencies, label, description);
	public ConnectionSettingDescriptor Add(string name, string alias, bool required, string label = null, string description = null) => this.Add(name, alias, required, null, null, label, description);
	public ConnectionSettingDescriptor Add(string name, string alias, bool required, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null) => this.Add(name, alias, required, null, dependencies, label, description);
	public ConnectionSettingDescriptor Add(string name, string alias, bool required, object defaultValue, string label = null, string description = null) => this.Add(name, alias, required, defaultValue, null, label, description);
	public ConnectionSettingDescriptor Add(string name, string alias, bool required, object defaultValue, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null)
	{
		var descriptor = new ConnectionSettingDescriptor(name, alias, null, required, defaultValue, label, description, dependencies);
		this.Add(descriptor);
		return descriptor;
	}

	public ConnectionSettingDescriptor Add(string name, string alias, Type type, string label = null, string description = null) => this.Add(name, alias, type, false, null, null, label, description);
	public ConnectionSettingDescriptor Add(string name, string alias, Type type, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null) => this.Add(name, alias, type, false, null, dependencies, label, description);
	public ConnectionSettingDescriptor Add(string name, string alias, Type type, object defaultValue, string label = null, string description = null) => this.Add(name, alias, type, false, defaultValue, null, label, description);
	public ConnectionSettingDescriptor Add(string name, string alias, Type type, object defaultValue, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null) => this.Add(name, alias, type, false, defaultValue, dependencies, label, description);
	public ConnectionSettingDescriptor Add(string name, string alias, Type type, bool required, string label = null, string description = null) => this.Add(name, alias, type, required, null, null, label, description);
	public ConnectionSettingDescriptor Add(string name, string alias, Type type, bool required, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null) => this.Add(name, alias, type, required, null, dependencies, label, description);
	public ConnectionSettingDescriptor Add(string name, string alias, Type type, bool required, object defaultValue, string label = null, string description = null) => this.Add(name, alias, type, required, defaultValue, null, label, description);
	public ConnectionSettingDescriptor Add(string name, string alias, Type type, bool required, object defaultValue, IEnumerable<ConnectionSettingDescriptor.Dependency> dependencies, string label = null, string description = null)
	{
		var descriptor = new ConnectionSettingDescriptor(name, alias, type, required, defaultValue, label, description, dependencies);
		this.Add(descriptor);
		return descriptor;
	}
	#endregion

	#region 内部方法
	internal ConnectionSettingDescriptor Add(PropertyInfo property)
	{
		if(property == null)
			throw new ArgumentNullException(nameof(property));

		if(!property.CanRead || !property.CanWrite || !property.SetMethod.IsPublic)
			return null;

		//忽略内置属性
		if(property.DeclaringType == typeof(Setting))
			return null;

		var alias = property.GetCustomAttribute<AliasAttribute>();
		var descriptor = new ConnectionSettingDescriptor(property.Name, alias?.Alias, property.PropertyType)
		{
			Label = GetDisplayName(property),
			Description = GetDescription(property),
			DefaultValue = GetDefaultValue(property)
		};

		var attribute = property.GetCustomAttribute<ConnectionSettingAttribute>();
		if(attribute != null)
		{
			descriptor.Format = attribute.Format;
			descriptor.Required = attribute.Required;

			if(attribute.GetOptions(out var options))
			{
				descriptor.Options.Clear();

				for(int i = 0; i < options.Length; i++)
					descriptor.Options.Add(options[i]);
			}

			if(attribute.GetDependencies(out var dependencies))
			{
				descriptor.Dependencies.Clear();

				for(int i = 0; i < attribute.Dependencies.Length; i++)
					descriptor.Dependencies.Add(dependencies[i]);
			}
		}

		this.Add(descriptor);
		return descriptor;

		static string GetDisplayName(PropertyInfo property)
		{
			var attribute = property.GetCustomAttribute<DisplayNameAttribute>();
			return attribute?.DisplayName;
		}

		static string GetDescription(PropertyInfo property)
		{
			var attribute = property.GetCustomAttribute<DescriptionAttribute>();
			return attribute?.Description;
		}

		static object GetDefaultValue(PropertyInfo property)
		{
			var attribute = property.GetCustomAttribute<DefaultValueAttribute>();

			if(attribute == null)
				return Common.TypeExtension.GetDefaultValue(property.PropertyType);
			else
				return Common.Convert.ConvertValue(attribute.Value, property.PropertyType, () => Common.TypeExtension.GetDefaultValue(property.PropertyType));
		}
	}
	#endregion
}