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
		try
		{
			if(location != null && resources.TryGetValue(location, out var resource))
			{
				value = resource.GetObject(name);
				return value != null;
			}

			value = null;
			return false;
		}
		catch
		{
			value = null;
			return false;
		}
	}

	private static bool TryGetResourceString(Dictionary<string, ResourceManager> resources, string location, string name, out string value)
	{
		try
		{
			if(location != null && resources.TryGetValue(location, out var resource))
			{
				value = resource.GetString(name);
				return value != null;
			}

			value = null;
			return false;
		}
		catch
		{
			value = null;
			return false;
		}
	}
	#endregion
}
