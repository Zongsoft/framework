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
using System.Reflection;

namespace Zongsoft.Components;

public static class HandlerUtility
{
	#region 常量定义
	private const string HANDLER_SUFFIX = "Handler";
	private const string HANDLER_VARIABLE = "[handler]";
	#endregion

	#region 公共方法
	public static string[] GetUrls(this IHandler handler)
	{
		var attributes = handler.GetType().GetCustomAttributes<HandlerAttribute>(true).ToArray();
		var templates = new string[attributes.Length];

		for(int i = 0; i < attributes.Length; i++)
		{
			if(string.IsNullOrEmpty(attributes[i].Template))
				templates[i] = GetName(handler.GetType());
			else
				templates[i] = attributes[i].Template.Replace(HANDLER_VARIABLE, GetName(handler.GetType()));
		}

		return templates;
	}
	#endregion

	#region 私有方法
	private static string GetName(Type type) => type != null && type.Name.EndsWith(HANDLER_SUFFIX) ? type.Name[..^7] : null;
	#endregion
}
