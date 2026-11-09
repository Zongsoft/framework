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
