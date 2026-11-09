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
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.Resources;

public class ResourceLocator(Resource resource) : IResourceLocator
{
	#region 成员字段
	private readonly Resource _resource = resource ?? throw new ArgumentNullException(nameof(resource));
	#endregion

	#region 公共方法
	public IEnumerable<string> Locate(Type origin) => this.Locate(ResourceUtility.GetLocation(origin));
	public IEnumerable<string> Locate(string origin)
	{
		//如果资源管理器中没有资源集，则无需定位
		if(_resource.Count == 0)
			yield break;

		//如果资源管理器中只有一个资源集，则只能定位它
		if(_resource.Count == 1)
			yield return _resource.Resources.First().BaseName;

		if(!string.IsNullOrEmpty(origin))
		{
			foreach(var location in GetLocations(origin))
				yield return location;
		}

		foreach(var location in GetLocations($"{_resource.Assembly.GetName().Name}"))
			yield return location;
	}
	#endregion

	#region 私有方法
	private static IEnumerable<string> GetLocations(string path)
	{
		if(string.IsNullOrEmpty(path))
			yield break;

		var parts = path.Split(Type.Delimiter);

		for(int i = 0; i < parts.Length; i++)
		{
			var location = string.Join(Type.Delimiter, parts, 0, parts.Length - i);

			yield return $"{location}.Properties.Resources";
			yield return $"{location}.Resources";
			yield return location;
		}
	}
	#endregion
}
