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
using System.Collections.Generic;

namespace Zongsoft.Configuration;

/// <summary>
/// 表示连接设置项的注解类。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class ConnectionSettingAttribute : Attribute
{
	#region 构造函数
	public ConnectionSettingAttribute(params string[] dependencies) : this(false, dependencies) { }
	public ConnectionSettingAttribute(bool required, params string[] dependencies)
	{
		this.Required = required;
		this.Dependencies = dependencies;
	}
	public ConnectionSettingAttribute(bool required, Type populator, params string[] dependencies)
	{
		this.Required = required;
		this.Populator = populator;
		this.Dependencies = dependencies;
	}

	public ConnectionSettingAttribute(Type populator, params string[] dependencies)
	{
		this.Populator = populator;
		this.Dependencies = dependencies;
	}
	public ConnectionSettingAttribute(Type populator, bool required, params string[] dependencies)
	{
		this.Required = required;
		this.Populator = populator;
		this.Dependencies = dependencies;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置设置项的格式字符串。</summary>
	public string Format { get; set; }

	/// <summary>获取或设置一个值，指示是否为必须项。</summary>
	public bool Required { get; set; }

	/// <summary>获取或设置设置项的转换到对应选项对象成员值的转换器(组装器)类型。</summary>
	public Type Populator { get; set; }

	/// <summary>获取或设置设置项的选项数组。</summary>
	/// <remarks>单个选项的格式为：<c>name[:value][|label[|description]]</c>。</remarks>
	public string[] Options { get; set; }

	/// <summary>获取或设置设置项的依赖数组。</summary>
	/// <remarks>单个依赖的格式为：<c>name[:value]</c>。</remarks>
	public string[] Dependencies { get; set; }
	#endregion

	#region 公共方法
	public ConnectionSettingDescriptor.Option[] GetOptions() => this.GetOptions(out var options) ? options : null;
	public bool GetOptions(out ConnectionSettingDescriptor.Option[] result)
	{
		if(this.Options != null && this.Options.Length > 0)
		{
			var options = new List<ConnectionSettingDescriptor.Option>();

			for(int i = 0; i < this.Options.Length; i++)
			{
				if(ConnectionSettingDescriptor.Option.TryParse(this.Options[i], out var option))
					options.Add(option);
			}

			if(options != null && options.Count > 0)
			{
				result = options.ToArray();
				return true;
			}
		}

		result = null;
		return false;
	}

	public ConnectionSettingDescriptor.Dependency[] GetDependencies() => this.GetDependencies(out var dependencies) ? dependencies : null;
	public bool GetDependencies(out ConnectionSettingDescriptor.Dependency[] result)
	{
		if(this.Dependencies != null && this.Dependencies.Length > 0)
		{
			var dependencies = new List<ConnectionSettingDescriptor.Dependency>();

			for(int i = 0; i < this.Dependencies.Length; i++)
			{
				if(ConnectionSettingDescriptor.Dependency.TryParse(this.Dependencies[i], out var dependency))
					dependencies.Add(dependency);
			}

			if(dependencies != null && dependencies.Count > 0)
			{
				result = dependencies.ToArray();
				return true;
			}
		}

		result = null;
		return false;
	}
	#endregion
}
