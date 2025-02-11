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
using System.Resources;
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Resources;

public partial class Resource : IResource
{
	#region 成员字段
	private IResourceLocator _locator;
	private readonly Assembly _assembly;
	private readonly Dictionary<string, ResourceManager> _resources;
	#endregion

	#region 构造函数
	public Resource(Assembly assembly, IResourceLocator locator = null)
	{
		const string RESOURCE_SUFFIX = ".resources";

		_assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
		_resources = new Dictionary<string, ResourceManager>();

		//获取程序集中的资源集名称数组
		var names = assembly.GetManifestResourceNames();

		for(int i = 0; i < names.Length; i++)
		{
			var name = names[i].Length > RESOURCE_SUFFIX.Length && names[i].EndsWith(RESOURCE_SUFFIX) ? names[i][..^RESOURCE_SUFFIX.Length] : names[i];
			_resources.Add(name, new ResourceManager(name, assembly));
		}

		//如果未指定资源定位器则构建一个默认资源定位器
		_locator = locator ?? new ResourceLocator(this);
	}
	#endregion

	#region 公共属性
	public IResourceLocator Locator
	{
		get => _locator;
		set => _locator = value ?? throw new ArgumentNullException();
	}
	#endregion

	#region 内部属性
	internal int Count => _resources.Count;
	internal protected Assembly Assembly => _assembly;
	internal protected ICollection<ResourceManager> Resources => _resources.Values;
	#endregion

	#region 公共方法
	public object GetObject(string name, string location = null) => this.TryGetObject(name, location, out var value) ? value : null;
	public string GetString(string name, string location = null) => this.TryGetString(name, location, out var value) ? value : null;

	public bool TryGetObject(string name, out object value) => this.TryGetObject(name, null, out value);
	public bool TryGetObject(string name, string location, out object value)
	{
		if(string.IsNullOrEmpty(name))
		{
			value = null;
			return false;
		}

		foreach(var path in _locator.Locate(location))
		{
			if(TryGetResourceObject(_resources, path, name, out value))
				return true;
		}

		value = null;
		return false;
	}

	public bool TryGetString(string name, out string value) => this.TryGetString(name, null, out value);
	public bool TryGetString(string name, string location, out string value)
	{
		if(string.IsNullOrEmpty(name))
		{
			value = null;
			return false;
		}

		foreach(var path in _locator.Locate(location))
		{
			if(TryGetResourceString(_resources, path, name, out value))
				return true;
		}

		value = null;
		return false;
	}
	#endregion

	#region 私有方法
	private static bool TryGetResourceObject(Dictionary<string, ResourceManager> resources, string location, string name, out object value)
	{
		if(location != null && resources.TryGetValue(location, out var resource))
		{
			value = resource.GetObject(name);
			return value != null;
		}

		value = null;
		return false;
	}

	private static bool TryGetResourceString(Dictionary<string, ResourceManager> resources, string location, string name, out string value)
	{
		if(location != null && resources.TryGetValue(location, out var resource))
		{
			value = resource.GetString(name);
			return value != null;
		}

		value = null;
		return false;
	}
	#endregion
}
