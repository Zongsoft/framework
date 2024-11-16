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
