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
 * Copyright (C) 2020-2024 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Components;

public class HandlerSelector
{
	#region 单例字段
	public static readonly HandlerSelector Default = new();
	#endregion

	#region 私有字段
	private readonly Dictionary<Type, string[]> _templates = new();
	#endregion

	#region 公共方法
	public IHandler GetHandler(IEnumerable<IHandler> handlers, string url)
	{
		if(handlers == null)
			return null;

		if(string.IsNullOrEmpty(url))
			return null;

		return handlers.FirstOrDefault(handler => this.GetUrls(handler).Contains(url));
	}
	#endregion

	#region 私有方法
	public string[] GetUrls(IHandler handler)
	{
		if(handler == null)
			return [];

		if(_templates.TryGetValue(handler.GetType(), out var templates))
			return templates;

		lock(_templates)
		{
			if(_templates.TryGetValue(handler.GetType(), out templates))
				return templates;

			return _templates[handler.GetType()] = HandlerUtility.GetUrls(handler);
		}
	}
	#endregion
}
