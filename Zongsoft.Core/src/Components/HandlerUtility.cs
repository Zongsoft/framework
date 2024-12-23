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
